using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amazon;
using Amazon.DynamoDBv2.Model;
using AmazonsAlexa.Unity.AlexaCommunicationModule;
using UnityEngine.SceneManagement;
public class Alexa : MonoBehaviour
{

    //TODO: explore using seperate lower privledge keys on unity end
    private const string publishKey = "pub-c-fb2047b1-3026-4af9-8dc0-b80bdebbbca7";
    private const string subscribeKey = "sub-c-7b26c48a-4467-11e9-8534-9add990cf553";
    private const string tableName = "Battleship";
    //TODO: reduce the scope of db write permissions
    private const string identityPoolId = "us-east-1:4d5b6d38-73a4-44be-bd57-67b565228974";
    private string AWSRegion = RegionEndpoint.USEast1.SystemName;
    private const bool debug = false;
    // TODO: use the value provided from alexa to support multiple games
    private const string channel = "XXXXX";
    private Dictionary<string, AttributeValue> attributes;
    private AmazonAlexaManager alexaManager;

    // Start is called before the first frame update
    void Start()
    {
        UnityInitializer.AttachToGameObject(this.gameObject);
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        alexaManager = new AmazonAlexaManager(publishKey, subscribeKey, channel, tableName, identityPoolId, AWSRegion, null, OnAlexaMessage, null, debug);
    }

    private void ConfirmSetup(GetSessionAttributesEventData eventData)
    {
        //Notify the skill that setup has completed by updating the skills persistant attributes (in DynamoDB)
        attributes = eventData.Values;
        attributes["SETUP_STATE"] = new AttributeValue { S = "COMPLETED" }; //Set SETUP_STATE attribute to a string, COMPLETED
        alexaManager.SetSessionAttributes(attributes, SetAttributesCallback);
    }


    private void OnAlexaMessage(HandleMessageEventData eventData)
    {
        //Listen for new messages from the Alexa skill
        Debug.Log("OnAlexaMessage");

        Dictionary<string, object> message = eventData.Message;

        //Get Session Attributes with in-line defined callback
        switch (message["type"] as string)
        {
            case "AlexaUserId":
                Debug.Log("AlexaUserId: " + message["message"]);
                alexaManager.alexaUserDynamoKey = message["message"] as string;
                break;
        }

        alexaManager.GetSessionAttributes((result) =>
        {
            if (result.IsError)
                Debug.LogError(eventData.Exception.Message);
            GameManager gm = FindObjectOfType<GameManager>();
            switch (message["type"] as string)
            {
                case "AlexaUserId":
                    ConfirmSetup(result);
                    break;
                case "PlaceRequest":
                    alexaManager.SendToAlexaSkill(gm.onMoveCommand(message["ship"] as string, message["col"] as string, message["row"] as string, message["orientation"] as string), null);
                    break;
                case "ConfirmPlacement":
                    alexaManager.SendToAlexaSkill(gm.onConfirmPlacementCommand(), null);
                    break;
                case "FireRequest":
                    alexaManager.SendToAlexaSkill(gm.onFireCommand(message["col"] as string, message["row"] as string), null);
                    break;
                default:
                    alexaManager.SendToAlexaSkill("Unrecognized message type!", null);
                    break;
            }
        });
    }

    private void SetAttributesCallback(SetSessionAttributesEventData eventData)
    {
        //Callback for when session attributes have been updated
        Debug.Log("OnSetAttributes");
        if (eventData.IsError)
            Debug.LogError(eventData.Exception.Message);
    }

}
