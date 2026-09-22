using MCEGold.Data.Services.Connector.Enums;
using MCEGold.Data.Services.Connector.RequestCriteria;
using MCEGold.Data.Services.Connector.Filters;

namespace MCEGold.Consumer.Request
{
    internal static class FormatRequestCriteria
    {
        internal static GetAssessments GetAssessmentsRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GetAssessments filters
            GetAssessments myGetAssessments = new GetAssessments();

            myGetAssessments.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 2 assessments per asset
            myGetAssessments.UserArea.LastNData = 5;

            // Limit to 100 assessments
            myGetAssessments.DataArea.Get.MaxItems = 100;

            // Create a new Assessments Criteria
            AssessmentsCriteria myAssessmentsCriteria = new AssessmentsCriteria();

            //An UUID filter to specify a particular Asset by its Asset UUID,
            //allowing the system to filter and retrieve data related to Assessments specific to that Asset.
            //myAssessmentsCriteria.AssetUUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "366c6e8d-88a6-4e9e-828f-9f16f0384a54"
            //};

            // An UUID filter to specify a particular Health Level Type by its Health Level Type UUID,
            // allowing the system to filter and retrieve data related to Assessments specific to that Health Level Type.
            //myAssessmentsCriteria.HealthLevelTypeUUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "78994a5e-b245-4a4e-9676-017b18750fa8"
            //};

            // An UTCDateTime filter to specify a particular Minimum Inclusive Assessed UTC Data Time,
            // allowing the system to filter and retrieve data related to Assessments specific to that date time filter.
            UTCDateTimeFilter myFirstAssessedFilter = new UTCDateTimeFilter
            {
                FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
                Value = "2015-01-01T20:02:00Z"
            };
            myAssessmentsCriteria.Assessed.Add(myFirstAssessedFilter);

            // An UTCDateTime filter to specify a particular Maximum Inclusive Assessed UTC Data Time,
            // allowing the system to filter and retrieve data related to Assessments specific to that date time filter.
            UTCDateTimeFilter mySecondAssessedFilter = new UTCDateTimeFilter
            {
                FilterType = FilterTypes.UTCDateTimeFilter.MaxInclusive,
                Value = "2025-01-01T20:02:00Z"
            };
            myAssessmentsCriteria.Assessed.Add(mySecondAssessedFilter);

            // A Numeric filter to specify a particular Minimum Inclusive Health Level Precise,
            // allowing the system to filter and retrieve data related to Assessments specific to that Health Level Precise.
            NumericFilter myFirstHealthLevelPreciseFilter = new NumericFilter
            {
                FilterType = FilterTypes.NumericFilter.MinInclusive,
                Value = 0.25
            };
            myAssessmentsCriteria.HealthLevelPrecise.Add(myFirstHealthLevelPreciseFilter);

            // A Numeric filter to specify a particular Maximum Inclusive Health Level Precise,
            // allowing the system to filter and retrieve data related to Assessments specific to that Health Level Precise.
            NumericFilter mySecondHealthLevelPreciseFilter = new NumericFilter
            {
                FilterType = FilterTypes.NumericFilter.Max,
                Value = 0.75
            };
            myAssessmentsCriteria.HealthLevelPrecise.Add(mySecondHealthLevelPreciseFilter);

            // Add Criteria to GetAssessments object
            myGetAssessments.DataArea.AssessmentsCriteria.Add(myAssessmentsCriteria);

            return myGetAssessments;
        }

        internal static GetAssets GetAssetsRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GetAssets filters
            GetAssets myGetAessts = new GetAssets();

            myGetAessts.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 10 return
            myGetAessts.DataArea.Get.MaxItems = 10;

            // Create a new Assets Criteria
            AssetsCriteria myAssetsCriteria = new AssetsCriteria();

            //// An UUID filter to specify a particular asset by its unique identifier (UUID),
            //// allowing the system to filter and retrieve data related to specific asset.
            //myAssetsCriteria.UUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "2CE86D0B-CB71-4977-A2BD-03891B304E12"
            //};

            //// An TypeUUID filter to specify a particular asset type by its asset type UUID,
            //// allowing the system to filter and retrieve data related to Assets specific to that asset type.
            //myAssetsCriteria.TypeUUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    // AC Induction Motor
            //    Value = "3ef8aae8-6f4e-49d6-b8ce-3eb289452b38"
            //};

            // An UUID filter to specify a particular Site by its Site UUID,
            // allowing the system to filter and retrieve Assts specific to that Site.
            UUIDFilter myFirstSiteUUIDFilter = new UUIDFilter()
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                Value = "7431B9CB-92E6-407B-A6CA-660AE95EC1DE"
            };
            myAssetsCriteria.SiteUUID.Add(myFirstSiteUUIDFilter);

            //UUIDFilter mySecondSiteUUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "3331B9CB-92E6-407B-A6CA-660AE95EC1DE"
            //};
            //myAssetsCriteria.SiteUUID.Add(mySecondSiteUUIDFilter);

            //// A Text filter to specify a particular asset by its serial number,
            //// allowing the system to filter and retrieve data related to Asset specific to that serial number.
            //TextFilter mySerialNumberFilter = new TextFilter()
            //{
            //    FilterType = FilterTypes.TextFilter.Equal,
            //    Value = "61655-7855"
            //};
            //myAssetsCriteria.SerialNumber.Add(mySerialNumberFilter);

            // Add Criteria to GetAssets object
            myGetAessts.DataArea.AssetsCriteria.Add(myAssetsCriteria);

            return myGetAessts;
        }

        internal static GetAssetSegmentEvents GetAssetSegmentEventsRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GetAssetSegmentEvents filters
            GetAssetSegmentEvents myGetAssetSegmentEvents = new GetAssetSegmentEvents();

            myGetAssetSegmentEvents.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 10 AssetSegmentEvents per Site 
            myGetAssetSegmentEvents.UserArea.LastNData = 10;

            // Limit to 100 AssetSegmentEvents for entire message
            myGetAssetSegmentEvents.DataArea.Get.MaxItems = 100;

            // Create a new Criteria
            AssetSegmentEventsCriteria myAssetSegmentEventsCriteria = new AssetSegmentEventsCriteria();

            // An UUID filter to specify a particular AssetSegmentEvent by its AssetSegmentEvent UUID,
            // allowing the system to filter and retrieve AssetSegmentEvents specific to that AssetSegmentEvent.
            //myAssetSegmentEventsCriteria.UUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "d3f3e725-a75b-419d-99c3-b5bd5ecb1a57"
            //};

            // An UUID filter to specify a particular Site by its Site UUID,
            // allowing the system to filter and retrieve AssetSegmentEvent specific to that Site.
            UUIDFilter myFirstSiteUUIDFilter = new UUIDFilter()
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                Value = "7431b9cb-92e6-407b-a6ca-660ae95ec1de"
            };
            myAssetSegmentEventsCriteria.SiteUUID.Add(myFirstSiteUUIDFilter);

            // An UUID filter to specify a particular Segment by its Segment UUID,
            // allowing the system to filter and retrieve AssetSegmentEvents specific to that Segment.
            //myAssetSegmentEventsCriteria.SegmentUUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "cd4ae399-1de9-4442-8d3a-d9858d29d7aa"
            //};

            // An UUID filter to specify a particular Asset by its Asset UUID,
            // allowing the system to filter and retrieve AssetSegmentEvents specific to that Asset.
            //myAssetSegmentEventsCriteria.AssetUUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "f75da147-4592-4d8c-889b-1b8ae9faad57"
            //};

            // A Text filter to specify a particular Asset by its Serial Number,
            // allowing the system to filter and retrieve data related AssetSegmentEvents specific to that Serial Number.
            //TextFilter mySerialNumberFilter = new TextFilter()
            //{
            //    FilterType = FilterTypes.TextFilter.Equal,
            //    Value = "HV123"
            //};
            //myAssetSegmentEventsCriteria.AssetSerialNumber.Add(mySerialNumberFilter);

            //// A UTCDateTime filter to specify a particular Minimum Inclusive Installed UTC Data Time,
            //// allowing the system to filter and retrieve data related AssetSegmentEvents specific to that date time filter.
            UTCDateTimeFilter myFirstInstalledFilter = new UTCDateTimeFilter
            {
                FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
                Value = "2026-01-15T16:32:00.00Z"
            };
            myAssetSegmentEventsCriteria.Installed.Add(myFirstInstalledFilter);

            UTCDateTimeFilter mySecondInstalledFilter = new UTCDateTimeFilter
            {
                FilterType = FilterTypes.UTCDateTimeFilter.MaxInclusive,
                Value = "2026-01-28T12:19:20.00Z"
            };
            myAssetSegmentEventsCriteria.Installed.Add(mySecondInstalledFilter);

            // A UTCDateTime filter to specify a particular Minimum Inclusive Removed UTC Data Time,
            // allowing the system to filter and retrieve data related AssetSegmentEvents specific to that date time filter.
            //UTCDateTimeFilter myFirstRemovedFilter = new UTCDateTimeFilter
            //{
            //    FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
            //    Value = "2025-08-06T14:40:53.000000000"
            //};
            //myAssetSegmentEventsCriteria.Removed.Add(myFirstRemovedFilter);

            // A Boolean filter to specify a particular Installation State,
            // allowing the system to filter and retrieve data related AssetSegmentEvents specific to that Installation State.
            BooleanFilter myFirstBooleanFilter = new BooleanFilter
            {
                Value = true
            };
            myAssetSegmentEventsCriteria.InstalledNow.Add(myFirstBooleanFilter);

            myGetAssetSegmentEvents.DataArea.AssetSegmentEventsCriteria.Add(myAssetSegmentEventsCriteria);

            return myGetAssetSegmentEvents;
        }

        internal static GetMeasurements GetMeasurementsRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GetMeasurements filters
            GetMeasurements myGetMeasurements = new GetMeasurements();

            myGetMeasurements.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 2 Measurements per Measurement Location
            //myGetMeasurements.UserArea.LastNData = 2;

            // Limit to 100 Measurements
            myGetMeasurements.DataArea.Get.MaxItems = 100;

            // Create a new Criteria
            MeasurementsCriteria myMeasurementsCriteria = new MeasurementsCriteria();

            // A SegmentUUID filter to specify a particular Segment by its Segment UUID,
            // allowing the system to filter and retrieve data related Measurements specific to that segment.
            myMeasurementsCriteria.SegmentUUID.UUIDFilter = new UUIDFilter
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                Value = "F94E96AC-7567-4E0B-9DA6-F47E735769D5"
            };

            // A MeasurementLocationUUID filter to specify a particular Measurement Location by its MeasurementLocation UUID,
            // allowing the system to filter and retrieve data related to Measurements specific to that measurement location.
            //myMeasurementsCriteria.MeasurementLocationUUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "F9ACD222-C24B-4412-B074-B2CD2E25FDB0"
            //};

            // A UTCDateTime filter to specify a particular Minimum Inclusive UTC Data Time,
            // allowing the system to filter and retrieve data related to Measurements specific to that date time filter.
            UTCDateTimeFilter myFirstRecordedFilter = new UTCDateTimeFilter
            {
                FilterType = FilterTypes.UTCDateTimeFilter.MinInclusive,
                Value = "2024-12-04 18:30:00.00Z"
            };
            myMeasurementsCriteria.Recorded.Add(myFirstRecordedFilter);

            // A UTCDateTime filter to specify a particular Maximum UTC Data Time,
            // allowing the system to filter and retrieve data related to Measurements specific to that date time filter.
            UTCDateTimeFilter mySecondRecordedFilter = new UTCDateTimeFilter
            {
                FilterType = FilterTypes.UTCDateTimeFilter.Max,
                Value = "2024-12-04 18:45:00.00Z"
            };
            myMeasurementsCriteria.Recorded.Add(mySecondRecordedFilter);

            // Add Criteria to GetMeasurements object
            myGetMeasurements.DataArea.MeasurementsCriteria.Add(myMeasurementsCriteria);

            return myGetMeasurements;
        }

        internal static GetMeasurementLocations GetMeasurementLocationsRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GetMeasurementLocations filters
            GetMeasurementLocations myGetMeasurementLocations = new GetMeasurementLocations();

            myGetMeasurementLocations.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 100 MeasurementLocations
            myGetMeasurementLocations.DataArea.Get.MaxItems = 100;

            // Create a new Criteria
            MeasurementLocationsCriteria myMeasurementLocationsCriteria = new MeasurementLocationsCriteria();

            //// A MeasurementLocationUUID filter to specify a particular Measurement Location by its UUID,
            //// allowing the system to filter and retrieve data related to Measurement Location specific to that UUID.
            //myMeasurementLocationsCriteria.UUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "D34EEEBA-6A37-4A35-85AF-080D3ED3313C"
            //};

            // An TypeUUID filter to specify a particular Measurement Location type by its type UUID,
            // allowing the system to filter and retrieve data related to Measurement Locations specific to that measurement location type.
            //myMeasurementLocationsCriteria.TypeUUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "1a0179b9-458f-437a-a73d-ac70b54355fe"
            //};

            // An UUID filter to specify a particular Site by its Site UUID,
            // allowing the system to filter and retrieve Measurement Locations specific to that Site.
            UUIDFilter myFirstSiteUUIDFilter = new UUIDFilter()
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                Value = "7431B9CB-92E6-407B-A6CA-660AE95EC1DE"
            };
            myMeasurementLocationsCriteria.SiteUUID.Add(myFirstSiteUUIDFilter);

            UUIDFilter mySecondSiteUUIDFilter = new UUIDFilter()
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                Value = "3331B9CB-92E6-407B-A6CA-660AE95EC1DE"
            };
            myMeasurementLocationsCriteria.SiteUUID.Add(mySecondSiteUUIDFilter);

            // A SegmentUUID filter to specify a particular Segment by its Segment UUID,
            // allowing the system to filter and retrieve data related Measurements specific to that segment.
            //myMeasurementLocationsCriteria.SegmentUUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "f94e96ac-7567-4e0b-9da6-f47e735769d5"
            //};

            // Add Criteria to GetMeasurements object
            myGetMeasurementLocations.DataArea.MeasurementLocationsCriteria.Add(myMeasurementLocationsCriteria);

            return myGetMeasurementLocations;
        }

        internal static GetSegments GetSegmentsRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GetSegments filters
            GetSegments myGetSegments = new GetSegments();

            myGetSegments.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 10 Sites
            myGetSegments.DataArea.Get.MaxItems = 10;

            // Create a new Criteria
            SegmentsCriteria mySegmentsCriteria = new SegmentsCriteria();

            // An UUID filter to specify a particular segment by its unique identifier (UUID),
            // allowing the system to filter and retrieve data related to specific segment.
            //mySegmentsCriteria.UUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "9e88dd92-f1cb-4330-bfde-4259367f3dc8"
            //};

            // An TypeUUID filter to specify a particular segment type by its segment type UUID,
            // allowing the system to filter and retrieve data related to Segments specific to that segment type.
            //mySegmentsCriteria.TypeUUID.UUIDFilter = new UUIDFilter()
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    // AC Induction Motor
            //    Value = "ba34c9f5-112e-41e1-940d-af03de9ee786"
            //};


            // An UUID filter to specify a particular site by its Site UUID,
            // allowing the system to filter and retrieve data related to Segments specific to that Site.
            UUIDFilter myFirstSiteFilter = new UUIDFilter
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                Value = "7431B9CB-92E6-407B-A6CA-660AE95EC1DE"
            };


            // Add filter to Criteria
            mySegmentsCriteria.SiteUUID.Add(myFirstSiteFilter);

            // Add Criteria to GetSegments object
            myGetSegments.DataArea.SegmentsCriteria.Add(mySegmentsCriteria);

            return myGetSegments;
        }

        internal static GetSites GetSitesRequest(PayloadProfile payloadProfile)
        {
            // Example to setup GstSites filters
            GetSites myGetSites = new GetSites();

            myGetSites.UserArea.MCEGold.PayloadProfile = payloadProfile;

            // Limit to 10 return
            myGetSites.DataArea.Get.MaxItems = 10;

            // Create a new Criteria
            SitesCriteria mySitesCriteria = new SitesCriteria();

            // An UUID filter to specify a particular Site by its unique identifier (UUID),
            // allowing the system to filter and retrieve data related to Site specific to that site.
            //mySitesCriteria.UUID.UUIDFilter = new UUIDFilter
            //{
            //    FilterType = FilterTypes.UUIDFilter.Equal,
            //    Value = "f6ea5116-df33-4956-9cee-1edc01f3f02d"
            //};

            // An TypeUUID filter to specify a particular segment type by its segment type UUID,
            // allowing the system to filter and retrieve data related to Segments specific to that segment type.
            mySitesCriteria.TypeUUID.UUIDFilter = new UUIDFilter()
            {
                FilterType = FilterTypes.UUIDFilter.Equal,
                // AC Induction Motor
                Value = "d99b1a17-ed17-4375-9e82-ccb99481dd8a"
            };

            // Add Criteria to GetSites object
            myGetSites.DataArea.SitesCriteria.Add(mySitesCriteria);
            

            return myGetSites;
        }

    }
}
