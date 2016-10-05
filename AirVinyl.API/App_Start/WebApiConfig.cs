using AirVinyl.Model;
using Microsoft.OData.Edm;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.OData.Batch;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;


namespace AirVinyl.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            //config.MapODataServiceRoute(
            //    "ODataRoute",
            //    "odata",
            //    GetEdmModel());
            
            // Batch support
            config.MapODataServiceRoute(
                "ODataRoute",
                "odata",
                GetEdmModel(),
                new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));

            config.EnableCors();

            config.EnsureInitialized();
        }

        private static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "AirVinyl";
            builder.ContainerName = "AirVinylContainer";

            builder.EntitySet<Person>("People");
            builder.EntitySet<RecordStore>("RecordStores");

            // function bound to RecordStore entity
            var isHighRatedFunction = builder.EntityType<RecordStore>()
               .Function("IsHighRated");

            isHighRatedFunction.Returns<bool>();
            isHighRatedFunction.Parameter<int>("minimumRating");
            isHighRatedFunction.Namespace = "AirVinyl.Functions";

            // function bound to RecordStore list
            var areRatedByFunction = builder.EntityType<RecordStore>().Collection
               .Function("AreRatedBy");

            areRatedByFunction.ReturnsCollectionFromEntitySet<RecordStore>("RecordStores");
            areRatedByFunction.CollectionParameter<int>("personIds");
            areRatedByFunction.Namespace = "AirVinyl.Functions";

            // unbound function(no entity type)
            var getHighRatedRecordStoresFunction = builder.Function("GetHighRatedRecordStores");

            getHighRatedRecordStoresFunction.Parameter<int>("minimumRating");
            getHighRatedRecordStoresFunction.ReturnsCollectionFromEntitySet<RecordStore>("RecordStores");
            getHighRatedRecordStoresFunction.Namespace = "AirVinyl.Functions";

            // action bound to RecordStore entity
            var rateAction = builder.EntityType<RecordStore>()
                .Action("Rate");

            rateAction.Returns<bool>();
            rateAction.Parameter<int>("rating");
            rateAction.Parameter<int>("personId");
            rateAction.Namespace = "AirVinyl.Actions";

            // action bound to RecordStore collection
            var removeRatingsAction = builder.EntityType<RecordStore>().Collection
                .Action("RemoveRatings");

            removeRatingsAction.Returns<bool>();
            removeRatingsAction.Parameter<int>("personId");
            removeRatingsAction.Namespace = "AirVinyl.Actions";

            // unbound action
            var removeRecordStoreRatingsAction = builder.Action("RemoveRecordStoreRatings");

            removeRecordStoreRatingsAction.Parameter<int>("personId");
            removeRecordStoreRatingsAction.Namespace = "AirVinyl.Actions";

            // "Tim" singleton
            builder.Singleton<Person>("Tim");

            return builder.GetEdmModel();
        }
    }
}
