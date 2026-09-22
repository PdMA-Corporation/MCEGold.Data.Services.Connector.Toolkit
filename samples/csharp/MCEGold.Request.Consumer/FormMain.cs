/* Purpose: This application serves as a simple Request-Response consumer for the MCEGold Data Service.
 *          It demonstrates how to use the MCEGold Client Connector to post requests and read responses
 *          from the MCEGold Data Service.
 * 
 * Copyright (c) 2024 PdMA Corporation
 * 
 */

using System;
using System.Windows.Forms;
using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.ResponseType;
using MCEGold.Data.Services.Connector.RequestCriteria;

namespace MCEGold.Consumer.Request
{
    public partial class FormMain : Form
    {
        // Initialize Consumer Request Service
        private readonly ConsumerRequestService myConsumerRequestService = new ConsumerRequestService();

        public FormMain()
        {
            InitializeComponent();
            SetConfigurations();
        }
        
        private void SetConfigurations()
        {
            //myConsumerRequestService.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.Basic;
            // Custom end-to-end message encryption is disabled for normal use; HTTPS is required for production
        }

        private void SubscribeNotificationService()
        {
            // Subscribe to the event
            myConsumerRequestService.NotificationService.MessageArrivedEvent += EventSubscriber;
        }

        private void UnsubscribeNotificationService()
        {
            // Unsubscribe to the event
            myConsumerRequestService.NotificationService.MessageArrivedEvent -= EventSubscriber;
        }

        private void EventSubscriber(object sender, RequestNoticificationArgs e)
        {
            // Handle the noticification event here
            // Calling MCEGold Connector method
            ReadResponseResponse myReadResponseResponse = myConsumerRequestService.ReadResponse(e.RequestMessageId);
            
            // MCEGold Connector Response
            textBoxStatusCode.Invoke(new Action(() => textBoxStatusCode.Text = myReadResponseResponse.StatusCode.ToString()));
            textBoxReasonPhrase.Invoke(new Action(() => textBoxReasonPhrase.Text = myReadResponseResponse.ReasonPhrase));
            textBoxResponse.Invoke(new Action(() => textBoxResponse.Text = myReadResponseResponse.ISBMHTTPResponse));

            if (myReadResponseResponse.StatusCode == 200)
            {
                textBoxMessageId.Invoke(new Action(() => textBoxMessageId.Text = myReadResponseResponse.MessageID));
                textBoxBODResponse.Invoke(new Action(() => textBoxBODResponse.Text = myReadResponseResponse.MessageContent));
            }
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            listBoxRequests.Items.Add("GetAssessments");
            listBoxRequests.Items.Add("GetAssets");
            listBoxRequests.Items.Add("GetAssetSegmentEvents");
            listBoxRequests.Items.Add("GetMeasurements");
            listBoxRequests.Items.Add("GetMeasurementLocations");
            listBoxRequests.Items.Add("GetSegments");
            listBoxRequests.Items.Add("GetSites");

            // Optionally set a default selected item
            listBoxRequests.SelectedIndex = 0;
            comboBoxPayloadProfile.Items.Clear();
            comboBoxPayloadProfile.Items.Add(PayloadProfile.Full);
            comboBoxPayloadProfile.Items.Add(PayloadProfile.Minimal);
            comboBoxPayloadProfile.SelectedIndex = 0;
        }
        private void buttonOpenSession_Click(object sender, EventArgs e)
        {
            //Calling MCEGold Connector method
            OpenConsumerRequestSessionResponse myOpenConsumerRequestSessionResponse = myConsumerRequestService.OpenConsumerRequestSession(textBoxHostName.Text, textBoxApiKey.Text, textBoxUserName.Text, textBoxPassword.Text);

            //// Calling MCEGold Connector method  
            //OpenConsumerRequestSessionResponse myOpenConsumerRequestSessionResponse = myConsumerRequestService.OpenConsumerRequestSession(textBoxHostName.Text, textBoxUserName.Text, textBoxPassword.Text);

            // MCEGold Connector Response
            textBoxStatusCode.Text = myOpenConsumerRequestSessionResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myOpenConsumerRequestSessionResponse.ReasonPhrase;
            textBoxResponse.Text = myOpenConsumerRequestSessionResponse.ISBMHTTPResponse;

            textBoxSessionId.Text = myOpenConsumerRequestSessionResponse.SessionID;
        }
        private void buttonPostRequest_Click(object sender, EventArgs e)
        {
            PostRequestResponse myPostRequestResponse = new PostRequestResponse();
            var payloadProfile =
                (PayloadProfile)comboBoxPayloadProfile.SelectedItem;

            switch (listBoxRequests.SelectedItem)
            {                     
                case "GetAssessments":
                    GetAssessments myGetAssessments =
                        FormatRequestCriteria.GetAssessmentsRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myGetAssessments);
                    break;

                case "GetAssets":
                    GetAssets myGetAssets = FormatRequestCriteria.GetAssetsRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myGetAssets);
                    break;

                case "GetAssetSegmentEvents":
                    GetAssetSegmentEvents myGetAssetSegmentEvents =
                        FormatRequestCriteria.GetAssetSegmentEventsRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myGetAssetSegmentEvents);
                    break;

                case "GetMeasurements":
                    GetMeasurements myGetMeasurements =
                        FormatRequestCriteria.GetMeasurementsRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myGetMeasurements);
                    break;

                case "GetMeasurementLocations":
                    GetMeasurementLocations myMeasurementLocations =
                        FormatRequestCriteria.GetMeasurementLocationsRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myMeasurementLocations);
                    break;

                case "GetSegments":
                    GetSegments myGetSegments = FormatRequestCriteria.GetSegmentsRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myGetSegments);
                    break;

                case "GetSites":
                    GetSites myGetSites = FormatRequestCriteria.GetSitesRequest(payloadProfile);
                    // Calling MCEGold Connector method
                    myPostRequestResponse = myConsumerRequestService.PostRequest(myGetSites);
                    break;

                default:
                    // Code to execute if expression doesn't match any case
                    return;
            }

            // MCEGold Connector Response
            textBoxStatusCode.Text = myPostRequestResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myPostRequestResponse.ReasonPhrase;
            textBoxResponse.Text = myPostRequestResponse.ISBMHTTPResponse;

            if (myPostRequestResponse.StatusCode == 201)
            {
                textBoxMessageId.Text = myPostRequestResponse.MessageID;
                textBoxRequestMessageId.Text = myPostRequestResponse.MessageID;
            }

        }
        private void buttonCloseSession_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            CloseConsumerRequestSessionResponse myCloseConsumerRequestSessionResponse = myConsumerRequestService.CloseConsumerRequestSession();

            //MCEGold Connector Response
            textBoxStatusCode.Text = myCloseConsumerRequestSessionResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myCloseConsumerRequestSessionResponse.ReasonPhrase;
            textBoxResponse.Text = myCloseConsumerRequestSessionResponse.ISBMHTTPResponse;
        }

        private void buttonRead_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            ReadResponseResponse myReadResponseResponse = myConsumerRequestService.ReadResponse(textBoxRequestMessageId.Text);

            // MCEGold Connector Response
            textBoxStatusCode.Text = myReadResponseResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myReadResponseResponse.ReasonPhrase;
            textBoxResponse.Text = myReadResponseResponse.ISBMHTTPResponse;

            if (myReadResponseResponse.StatusCode == 200)
            {
                textBoxMessageId.Text = myReadResponseResponse.MessageID;
                textBoxBODResponse.Text = myReadResponseResponse.MessageContent;
            }
        }
        private void buttonRemove_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            RemoveResponseResponse myRemoveResponseResponse = myConsumerRequestService.RemoveResponse(textBoxRequestMessageId.Text);

            // MCEGold Connector Response
            textBoxStatusCode.Text = myRemoveResponseResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myRemoveResponseResponse.ReasonPhrase;
            textBoxResponse.Text = myRemoveResponseResponse.ISBMHTTPResponse;

            textBoxBODResponse.Text = "";
            textBoxMessageId.Text = "";
        }

        private void checkBoxNotificationService_CheckedChanged(object sender, EventArgs e)
        {
            switch (checkBoxNotificationService.Checked)
            {
                case true:
                    SubscribeNotificationService();
                    myConsumerRequestService.NotificationService.AutoRemove = true;
                    checkBoxAutoRemove.Checked = true;
                    checkBoxAutoRemove.Visible = true;
                    buttonRemove.Enabled = false;
                    buttonRead.Enabled = false ;
                    break;
                case false:
                    UnsubscribeNotificationService();
                    checkBoxAutoRemove.Visible = false;
                    buttonRemove.Enabled = true;
                    buttonRead.Enabled = true;
                    break;
            }
        }

        private void checkBoxAutoRemove_CheckedChanged(object sender, EventArgs e)
        {
            switch (checkBoxAutoRemove.Checked)
            {
                case true:
                    myConsumerRequestService.NotificationService.AutoRemove = true;
                    buttonRemove.Enabled = false;
                    break;
                case false:
                    myConsumerRequestService.NotificationService.AutoRemove = false;
                    buttonRemove.Enabled = true;
                    break;
            }
        }

        private void listBoxRequests_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }
}
