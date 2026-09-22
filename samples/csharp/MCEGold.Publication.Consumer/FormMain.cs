/* Purpose: This application serves as a simple Publication Consumer for the MCEGold Data Service.
 *          It demonstrates how to use the MCEGold Client Connector to read publications from the
 *          MCEGold Data Service.   
 * 
 * Copyright (c) 2024 PdMA Corporation
 * 
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using MCEGold.Data.Services.Connector;
using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.ResponseType;
using MCEGold.Data.Services.Connector.ServerOptions;
using MCEGold.Consumer.Publication;
using System.Linq;

namespace MCEGold.Publication.Consumer
{
    public partial class FormMain : Form
    {
        // Initialize Consumer Publication Service
        ConsumerPublicationService myConsumerPublicationService = new ConsumerPublicationService();
        
        public FormMain()
        {
            InitializeComponent();
            SetConfigurations();
        }

        private void SetConfigurations()
        {
            //myConsumerPublicationService.Configurations.AuthenticationSchemeType = AuthenticationSchemeType.Basic;
            //myConsumerPublicationService.Configurations.IsEndToEndMessageEncryptionEnabled = false;
        }

        private void SubscribeNotificationService()
        {
            // Subscribe to the event
            myConsumerPublicationService.NotificationService.MessageArrivedEvent += EventSubscriber;
        }

        private void UnsubscribeNotificationService()
        {
            // Unsubscribe to the event
            myConsumerPublicationService.NotificationService.MessageArrivedEvent -= EventSubscriber;
        }
        private void EventSubscriber(object sender, PublicationNoticificationArgs e)
        {
            // Handle the event here
            // Calling MCEGold Connector method
            ReadPublicationResponse myReadPublicationResponse = myConsumerPublicationService.ReadPublication();

            //ISBM Adapter Response
            if (textBoxStatusCode.InvokeRequired)
            {
                // Use Control.Invoke to marshal the UI update to the UI thread
                textBoxStatusCode.Invoke(new Action(() => textBoxStatusCode.Text = myReadPublicationResponse.StatusCode.ToString()));
                textBoxReasonPhrase.Invoke(new Action(() => textBoxReasonPhrase.Text = myReadPublicationResponse.ReasonPhrase));
                textBoxResponse.Invoke(new Action(() => textBoxResponse.Text = myReadPublicationResponse.ISBMHTTPResponse));

                if (myReadPublicationResponse.StatusCode == 200)
                {
                    textBoxMessageID.Invoke(new Action(() => textBoxMessageID.Text = myReadPublicationResponse.MessageID));
                    textBoxTopic.Invoke(new Action(() => textBoxTopic.Text = myReadPublicationResponse.Topics[0]));
                    
                    // Create a new ListBox item
                    string displayText = "Message Id :" + myReadPublicationResponse.MessageID + " Topic :" + myReadPublicationResponse.Topics[0];
                    string tagText = myReadPublicationResponse.MessageContent;

                    var newItem = new ListBoxItem { Text = displayText, Tag = tagText };
                    ListBoxPublication.Invoke(new Action(() => ListBoxPublication.Items.Add(newItem)));
                   
                    // Select the newly added item
                    ListBoxPublication.Invoke(new Action(() => ListBoxPublication.SelectedItem = newItem));


                    ListBoxPublication.Invoke(new Action(() => ListBoxPublication.TopIndex = ListBoxPublication.Items.Count - 1));

                    int itemsCount = (int)ListBoxPublication.Invoke(new Func<int>(() => ListBoxPublication.Items.Count));
                    if (itemsCount >= 20)
                    {
                        ListBoxPublication.Invoke(new Action(() => ListBoxPublication.Items.RemoveAt(0)));
                        ListBoxPublication.Invoke(new Action(() => ListBoxPublication.TopIndex = ListBoxPublication.Items.Count - 1));
                    }

                    // Transform the received CCOM message to PowerBI style object lists.
                    (List<Segment> segments, List<MeasurementLocation> measurementLocations, List<Measurement> measurements) = Transformation.TransformToPowerBI(myReadPublicationResponse.MessageContent);
                    
                    UpdateGauges(measurements);
                        
                }
            }
            else
            {
                textBoxStatusCode.Text = myReadPublicationResponse.StatusCode.ToString();
                textBoxReasonPhrase.Text = myReadPublicationResponse.ReasonPhrase;
                textBoxResponse.Text = myReadPublicationResponse.ISBMHTTPResponse;

                if (myReadPublicationResponse.StatusCode == 200)
                {
                    textBoxMessageID.Text = myReadPublicationResponse.MessageID;
                    textBoxTopic.Text = myReadPublicationResponse.Topics[0];
                    
                    // Create a new ListBox item
                    string displayText = myReadPublicationResponse.MessageID + " - " + myReadPublicationResponse.Topics[0];
                    string tagText = myReadPublicationResponse.MessageContent;

                    var newItem = new ListBoxItem { Text = displayText, Tag = tagText };
                    ListBoxPublication.Items.Add(newItem);
                    // Select the newly added item
                    ListBoxPublication.SelectedItem = newItem;
                    ListBoxPublication.TopIndex = ListBoxPublication.Items.Count - 1;
                    
                    int itemsCount = (int)ListBoxPublication.Items.Count;
                    if (itemsCount >= 20)
                    {
                        ListBoxPublication.Items.RemoveAt(0);
                        ListBoxPublication.TopIndex = ListBoxPublication.Items.Count - 1;
                    }
                }
            }
        }

        // Custom class to hold the text and tag
        public class ListBoxItem
        {
            public string Text { get; set; }
            public string Tag { get; set; }

            // Override ToString() to display the Text in the ListBox
            public override string ToString()
            {
                return Text;
            }
        }

        private void buttonOpenSession_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            OpenSubscriptionSessionResponse myOpenSubscriptionSessionResponse = myConsumerPublicationService.OpenSubscriptionSession(textBoxHostName.Text, textBoxApiKey.Text, textBoxUserName.Text, textBoxPassword.Text);

            // MCEGold Connector Response
            textBoxStatusCode.Text = myOpenSubscriptionSessionResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myOpenSubscriptionSessionResponse.ReasonPhrase;
            textBoxResponse.Text = myOpenSubscriptionSessionResponse.ISBMHTTPResponse;

            textBoxSessionId.Text = myOpenSubscriptionSessionResponse.SessionID;
        }

        private void buttonCloseSession_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            CloseSubscriptionSessionResponse myCloseSubscriptionSessionResponse = myConsumerPublicationService.CloseSubscriptionSession();

            // MCEGold Connector Response
            textBoxStatusCode.Text = myCloseSubscriptionSessionResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myCloseSubscriptionSessionResponse.ReasonPhrase;
            textBoxResponse.Text = myCloseSubscriptionSessionResponse.ISBMHTTPResponse;

            if (myCloseSubscriptionSessionResponse.StatusCode == 204)
            {
                textBoxSessionId.Text = "";
                textBoxMessageID.Text = "";
                textBoxTopic.Text = "";
                textBoxBOD.Text = "";
                ListBoxPublication.Items.Clear();
            }
        }

        private void buttonRead_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            ReadPublicationResponse myReadPublicationResponse = myConsumerPublicationService.ReadPublication();

            // MCEGold Connector Response
            textBoxStatusCode.Text = myReadPublicationResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myReadPublicationResponse.ReasonPhrase;
            textBoxResponse.Text = myReadPublicationResponse.ISBMHTTPResponse;

            if (myReadPublicationResponse.StatusCode == 200)
            {
                textBoxMessageID.Text = myReadPublicationResponse.MessageID;
                textBoxTopic.Text = myReadPublicationResponse.Topics[0];
                textBoxBOD.Text = myReadPublicationResponse.MessageContent;
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            // Calling MCEGold Connector method
            RemovePublicationResponse myRemovePublicationResponse = myConsumerPublicationService.RemovePublication();

            // MCEGold Connector Response
            textBoxStatusCode.Text = myRemovePublicationResponse.StatusCode.ToString();
            textBoxReasonPhrase.Text = myRemovePublicationResponse.ReasonPhrase;
            textBoxResponse.Text = myRemovePublicationResponse.ISBMHTTPResponse;

            textBoxBOD.Text = "";
            textBoxMessageID.Text = "";
        }

        private void ListBoxPublication_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            ListBoxItem selectedItem = (ListBoxItem)ListBoxPublication.SelectedItem;
            textBoxBOD.Text = selectedItem.Tag;

            // Transform the received CCOM message to PowerDI style object lists.
            (List<Segment> segments, List<MeasurementLocation> measurementLocations, List<Measurement> measurements) = Transformation.TransformToPowerBI(selectedItem.Tag);

            UpdateGauges(measurements);
        }

        private void UpdateGauges(List<Measurement> measurements)
        {
            if (textBoxStatusCode.InvokeRequired)
            {
                textBoxEfficiency.Invoke(new Action(() => textBoxEfficiency.Text = measurements
                        .FirstOrDefault(m => m.MeasurementLocationName == "Efficiency Calc. (%), Stator")?.Value
                        .ToString() ?? "N/A"));

                textBoxFLA.Invoke(new Action(() => textBoxFLA.Text = measurements
                            .FirstOrDefault(m => m.MeasurementLocationName == "% FLA, Stator")?.Value
                            .ToString() ?? "N/A"));

                textBoxHP.Invoke(new Action(() => textBoxHP.Text = measurements
                            .FirstOrDefault(m => m.MeasurementLocationName == "Output Power Calc. (HP), Stator")?.Value
                            .ToString() ?? "N/A"));
            }
            else
            {
                textBoxEfficiency.Text = measurements
                        .FirstOrDefault(m => m.MeasurementLocationName == "Efficiency Calc. (%), Stator")?.Value
                        .ToString() ?? "N/A";

                textBoxFLA.Text = measurements
                            .FirstOrDefault(m => m.MeasurementLocationName == "% FLA, Stator")?.Value
                            .ToString() ?? "N/A";

                textBoxHP.Text = measurements
                            .FirstOrDefault(m => m.MeasurementLocationName == "Output Power Calc. (HP), Stator")?.Value
                            .ToString() ?? "N/A";
            }
        }
        //private void UpdateGauges(List<Measurement> measurements)
        //{
        //    if (textBoxStatusCode.InvokeRequired)
        //    {
        //        var EfficiencyValues = GetValuesByShortName(BODMessage, "Efficiency Calc. (%), Stator");
        //        if (EfficiencyValues.Count > 0)
        //        {
        //            textBoxEfficiency.Invoke(new Action(() => textBoxEfficiency.Text = EfficiencyValues[0].ToString()));
        //        }
        //        else
        //        {
        //            textBoxEfficiency.Invoke(new Action(() => textBoxEfficiency.Text = "N/A"));
        //        }
        //        var FLAValues = GetValuesByShortName(BODMessage, "% FLA, Stator");
        //        if (FLAValues.Count > 0)
        //        {
        //            textBoxFLA.Invoke(new Action(() => textBoxFLA.Text = FLAValues[0].ToString()));
        //        }
        //        else
        //        {
        //            textBoxFLA.Invoke(new Action(() => textBoxFLA.Text = "N/A"));
        //        }
        //        var HPValues = GetValuesByShortName(BODMessage, "Output Power Calc. (HP), Stator");
        //        if (HPValues.Count > 0)
        //        {
        //            textBoxHP.Invoke(new Action(() => textBoxHP.Text = HPValues[0].ToString()));
        //        }
        //        else
        //        {
        //            textBoxHP.Invoke(new Action(() => textBoxHP.Text = "N/A"));
        //        }
        //    }
        //    else
        //    {
        //        var EfficiencyValues = GetValuesByShortName(BODMessage, "Efficiency Calc. (%), Stator");
        //        if (EfficiencyValues.Count > 0)
        //        {
        //            textBoxEfficiency.Text = EfficiencyValues[0].ToString();
        //        }
        //        else
        //        {
        //            textBoxEfficiency.Text = "N/A";
        //        }
        //        var FLAValues = GetValuesByShortName(BODMessage, "% FLA, Stator");
        //        if (FLAValues.Count > 0)
        //        {
        //            textBoxFLA.Text = FLAValues[0].ToString();
        //        }
        //        else
        //        {
        //            textBoxFLA.Text = "N/A";
        //        }
        //        var HPValues = GetValuesByShortName(BODMessage, "Output Power Calc. (HP), Stator");
        //        if (HPValues.Count > 0)
        //        {
        //            textBoxHP.Text = HPValues[0].ToString();
        //        }
        //        else
        //        {
        //            textBoxHP.Text = "N/A";
        //        }
        //    }
        //}

        //private void UpdateGauges(string BODMessage)
        //{
        //    if (textBoxStatusCode.InvokeRequired)
        //    {
        //        var EfficiencyValues = GetValuesByShortName(BODMessage, "Efficiency Calc. (%), Stator");
        //        if (EfficiencyValues.Count > 0)
        //        {
        //            textBoxEfficiency.Invoke(new Action(() => textBoxEfficiency.Text = EfficiencyValues[0].ToString()));
        //        }
        //        else
        //        {
        //            textBoxEfficiency.Invoke(new Action(() => textBoxEfficiency.Text = "N/A"));
        //        }
        //        var FLAValues = GetValuesByShortName(BODMessage, "% FLA, Stator");
        //        if (FLAValues.Count > 0)
        //        {
        //            textBoxFLA.Invoke(new Action(() => textBoxFLA.Text = FLAValues[0].ToString()));
        //        }
        //        else 
        //        {
        //            textBoxFLA.Invoke(new Action(() => textBoxFLA.Text = "N/A"));
        //        }
        //        var HPValues = GetValuesByShortName(BODMessage, "Output Power Calc. (HP), Stator");
        //        if (HPValues.Count > 0)
        //        {
        //            textBoxHP.Invoke(new Action(() => textBoxHP.Text = HPValues[0].ToString()));
        //        }
        //        else
        //        {
        //            textBoxHP.Invoke(new Action(() => textBoxHP.Text = "N/A"));
        //        }
        //    }
        //    else
        //    {
        //        var EfficiencyValues = GetValuesByShortName(BODMessage, "Efficiency Calc. (%), Stator");
        //        if (EfficiencyValues.Count > 0)
        //        {
        //            textBoxEfficiency.Text = EfficiencyValues[0].ToString();
        //        }
        //        else 
        //        {
        //            textBoxEfficiency.Text = "N/A";
        //        }
        //        var FLAValues = GetValuesByShortName(BODMessage, "% FLA, Stator");
        //        if (FLAValues.Count > 0)
        //        {
        //            textBoxFLA.Text = FLAValues[0].ToString();
        //        }
        //        else
        //        {
        //            textBoxFLA.Text = "N/A";
        //        }
        //        var HPValues = GetValuesByShortName(BODMessage, "Output Power Calc. (HP), Stator");
        //        if (HPValues.Count > 0)
        //        {
        //            textBoxHP.Text = HPValues[0].ToString();
        //        }
        //        else
        //        {
        //            textBoxHP.Text = "N/A";
        //        }
        //    }

        //}
        //private static List<double> GetValuesByShortName(string jsonString, string shortName)
        //{
        //    var values = new List<double>();
        //    try
        //    {
        //        // Parse the JSON string into a JObject
        //        var jsonObject = JObject.Parse(jsonString);

        //        // Exit routine if no data                
        //        Int16 recordSetCount = (Int16)jsonObject["syncMeasurements"]["dataArea"]["sync"]["recordSetCount"];
        //        if (recordSetCount == 0)
        //        {
        //            return values;
        //        }

        //        // Select all measurements
        //        var measurements = jsonObject["syncMeasurements"]?["dataArea"]?["measurements"];

        //        if (measurements != null)
        //        {
        //            foreach (var measurement in measurements)
        //            {
        //                var locationShortNames = measurement["measurementLocation"]?["shortNames"];

        //                if (locationShortNames != null)
        //                {
        //                    foreach (var name in locationShortNames)
        //                    {
        //                        if (name["text"]?.ToString() == shortName)
        //                        {
        //                            // Try to get 'measure', 'percentage', and 'numeric' values
        //                            var measureValue = GetValueFromToken(measurement["measurement"]?[0]?["data"]?["measure"]);
        //                            if (measureValue != null)
        //                            {
        //                                values.Add(measureValue.Value);
        //                            }

        //                            var percentageValue = GetValueFromToken(measurement["measurement"]?[0]?["data"]?["percentage"]);
        //                            if (percentageValue != null)
        //                            {
        //                                values.Add(percentageValue.Value);
        //                            }

        //                            var numericValue = GetValueFromToken(measurement["measurement"]?[0]?["data"]?["number"]);
        //                            if (numericValue != null)
        //                            {
        //                                values.Add(numericValue.Value);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        return values;
        //    }
        //    catch (Exception ex) 
        //    {
        //        // Invalid BOD format
        //        return values;
        //    }
        //}

        //private static double? GetValueFromToken(JToken token)
        //{
        //    if (token == null)
        //    {
        //        return null; // Return null if the token is null
        //    }

        //    // If token["value"] exists and is an object, extract "numeric"
        //    if (token["value"] != null && token["value"].Type == JTokenType.Object)
        //    {
        //        JToken numericToken = token["value"]["numeric"];
        //        if (numericToken != null && double.TryParse(numericToken.ToString(), out double numericValue))
        //        {
        //            return numericValue;
        //        }
        //    }

        //    // If token["value"] exists and is a direct number
        //    if (token["value"] != null && double.TryParse(token["value"].ToString(), out double directValue))
        //    {
        //        return directValue;
        //    }

        //    // If token has a direct "numeric" field
        //    if (token["numeric"] != null && double.TryParse(token["numeric"].ToString(), out double standaloneNumeric))
        //    {
        //        return standaloneNumeric;
        //    }

        //    return null; // Return null if neither condition is met
        //}

        private void checkBoxNotificationService_CheckedChanged(object sender, EventArgs e)
        {
            switch (checkBoxNotificationService.Checked)
            {
                case true:
                    SubscribeNotificationService();
                    myConsumerPublicationService.NotificationService.AutoRemove = true;
                    checkBoxAutoRemove.Checked = true;
                    checkBoxAutoRemove.Visible = true;
                    buttonRemove.Enabled = false;
                    buttonRead.Enabled = false;
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
                    myConsumerPublicationService.NotificationService.AutoRemove = true;
                    buttonRemove.Enabled = false;
                    break;
                case false:
                    myConsumerPublicationService.NotificationService.AutoRemove = false;
                    buttonRemove.Enabled = true;
                    break;
            }
        }
    }
}
