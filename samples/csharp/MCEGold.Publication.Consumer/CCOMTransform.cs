using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCEGold.Consumer.Publication
{
    internal class Transform
    {
        public static (List<Segment>, List<MeasurementLocation>, List<Measurement>) TransformJson(string jsonContent)
        {
            List<Segment> segments = new List<Segment>();
            List<MeasurementLocation> locations = new List<MeasurementLocation>();
            List<Measurement> measurements = new List<Measurement>();

            //string jsonContent = File.ReadAllText(jsonFilePath);
            JObject jsonObject = JObject.Parse(jsonContent);

            var measurementData = jsonObject["syncMeasurements"]?["dataArea"]?["measurements"];
            if (measurementData != null)
            {
                foreach (var measurement in measurementData)
                {
                    string locationUUID = measurement["measurementLocation"]?["UUID"]?.ToString();
                    string measurementLocationName = measurement["measurementLocation"]?["shortNames"]?[0]?["text"]?.ToString();

                    var dataPoints = measurement["measurement"];
                    if (dataPoints != null)
                    {
                        foreach (var dataPoint in dataPoints)
                        {
                            string measurementUUID = dataPoint["UUID"]?.ToString();
                            DateTime timestamp = DateTime.Parse(dataPoint["recorded"]?["dateTime"]?.ToString());

                            string segmentUUID = dataPoint["segment"]?["UUID"]?.ToString();
                            string segmentName = dataPoint["segment"]?["shortNames"]?[0]?["text"]?.ToString();

                            if (!string.IsNullOrEmpty(segmentUUID) && !segments.Exists(s => s.SegmentUUID == segmentUUID))
                            {
                                segments.Add(new Segment
                                {
                                    SegmentUUID = segmentUUID,
                                    SegmentName = segmentName
                                });
                            }

                            if (!string.IsNullOrEmpty(locationUUID) && !locations.Exists(l => l.LocationUUID == locationUUID))
                            {
                                locations.Add(new MeasurementLocation
                                {
                                    LocationUUID = locationUUID,
                                    MeasurementLocationName = measurementLocationName,
                                    SegmentUUID = segmentUUID
                                });
                            }

                            double value = 0.0;
                            string unitOfMeasure = "N/A";

                            if (dataPoint["data"]?["percentage"]?["numeric"] != null)
                            {
                                value = dataPoint["data"]["percentage"]["numeric"].Value<double>();
                                unitOfMeasure = "%";
                            }
                            else if (dataPoint["data"]?["number"]?["numeric"] != null)
                            {
                                value = dataPoint["data"]["number"]["numeric"].Value<double>();
                            }
                            else if (dataPoint["data"]?["measure"]?["value"]?["numeric"] != null)
                            {
                                value = dataPoint["data"]["measure"]["value"]["numeric"].Value<double>();
                                unitOfMeasure = dataPoint["data"]["measure"]["UnitOfMeasure"]?["shortNames"]?[0]?["text"]?.ToString() ?? "Unknown Unit";
                            }

                            measurements.Add(new Measurement
                            {
                                MeasurementUUID = measurementUUID,
                                MeasurementName = measurementLocationName,
                                LocationUUID = locationUUID,
                                Timestamp = timestamp,
                                Value = value,
                                UnitOfMeasure = unitOfMeasure
                            });
                        }
                    }
                }
            }

            return (segments, locations, measurements);
        }

        public class Segment
        {
            public string SegmentUUID { get; set; }
            public string SegmentName { get; set; }
        }

        public class MeasurementLocation
        {
            public string LocationUUID { get; set; }
            public string MeasurementLocationName { get; set; }
            public string SegmentUUID { get; set; }  // Foreign Key
        }

        public class Measurement
        {
            public string MeasurementUUID { get; set; }
            public string MeasurementName { get; set; }
            public string LocationUUID { get; set; } // Foreign Key
            public DateTime Timestamp { get; set; }
            public double Value { get; set; }
            public string UnitOfMeasure { get; set; }
        }
    }
}
