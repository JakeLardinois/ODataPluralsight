using AirVinyl.API.Helpers;
using AirVinyl.DataAccessLayer;
using AirVinyl.Model;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace AirVinyl.API.Controllers
{
    public class RecordStoresController : ODataController
    {
        // context
        private AirVinylDbContext _ctx = new AirVinylDbContext();

        // GET odata/RecordStores
        [EnableQuery]
        public IHttpActionResult Get()
        {
            return Ok(_ctx.RecordStores);
        }

        // GET odata/RecordStores(key)
        [EnableQuery]
        public IHttpActionResult Get([FromODataUri] int key)
        {
            var recordStores = _ctx.RecordStores.Where(p => p.RecordStoreId == key);

            if (!recordStores.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(recordStores));
        }

        [HttpGet]
        [ODataRoute("RecordStores({key})/Tags")]
        [EnableQuery]
        public IHttpActionResult GetRecordStoreTagsProperty([FromODataUri] int key)
        {
            // no Include necessary for EF - Tags isn't a navigation property 
            // in the entity model.  
            var recordStore = _ctx.RecordStores
                .FirstOrDefault(p => p.RecordStoreId == key);

            if (recordStore == null)
            {
                return NotFound();
            }

            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();
            var collectionPropertyValue = recordStore.GetValue(collectionPropertyToGet);

            // return the collection of tags
            return this.CreateOKHttpActionResult(collectionPropertyValue);
        }

        [HttpPost]
        [ODataRoute("RecordStores")]
        public IHttpActionResult CreateRecordStore(RecordStore recordStore)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // add the RecordStore
            _ctx.RecordStores.Add(recordStore);
            _ctx.SaveChanges();

            // return the created RecordStore 
            return Created(recordStore);
        }

        [HttpPatch]
        [ODataRoute("RecordStores({key})")]
        [ODataRoute("RecordStores({key})/AirVinyl.Model.SpecializedRecordStore")]
        public IHttpActionResult UpdateRecordStorePartially([FromODataUri] int key, Delta<RecordStore> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // find a matching record store
            var currentRecordStore = _ctx.RecordStores.FirstOrDefault(p => p.RecordStoreId == key);

            // if the record store isn't found, return NotFound
            if (currentRecordStore == null)
            {
                return NotFound();
            }

            patch.Patch(currentRecordStore);
            _ctx.SaveChanges();

            // return NoContent
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("RecordStores({key})")]
        [ODataRoute("RecordStores({key})/AirVinyl.Model.SpecializedRecordStore")]
        public IHttpActionResult DeleteRecordStore([FromODataUri] int key)
        {
            var currentRecordStore = _ctx.RecordStores.Include("Ratings")
                .FirstOrDefault(p => p.RecordStoreId == key);
            if (currentRecordStore == null)
            {
                return NotFound();
            }

            currentRecordStore.Ratings.Clear();
            _ctx.RecordStores.Remove(currentRecordStore);
            _ctx.SaveChanges();

            // return NoContent
            return StatusCode(HttpStatusCode.NoContent);
        }
        
        [HttpGet]
        [ODataRoute("RecordStores({key})/AirVinyl.Functions.IsHighRated(minimumRating={minimumRating})")]
        public bool IsHighRated([FromODataUri] int key, int minimumRating)
        {
            // get the RecordStore
            var recordStore = _ctx.RecordStores
                .FirstOrDefault(p => p.RecordStoreId == key
                    && p.Ratings.Any()
                    && (p.Ratings.Sum(r => r.Value) / p.Ratings.Count) >= minimumRating);

            return (recordStore != null);
        }

        [HttpGet]
        [ODataRoute("RecordStores/AirVinyl.Functions.AreRatedBy(personIds={personIds})")]
        public IHttpActionResult AreRatedBy([FromODataUri] IEnumerable<int> personIds)
        {
            // get the RecordStores
            var recordStores = _ctx.RecordStores
                .Where(p => p.Ratings.Any(r => personIds.Contains(r.RatedBy.PersonId)));

            return this.CreateOKHttpActionResult(recordStores);
        }

        [HttpGet]
        [ODataRoute("GetHighRatedRecordStores(minimumRating={minimumRating})")]
        public IHttpActionResult GetHighRatedRecordStores([FromODataUri] int minimumRating)
        {
            // get the RecordStores
            var recordStores = _ctx.RecordStores
                .Where(p => p.Ratings.Any()
                    && (p.Ratings.Sum(r => r.Value) / p.Ratings.Count) >= minimumRating);

            return this.CreateOKHttpActionResult(recordStores);
        }

        [HttpPost]
        [ODataRoute("RecordStores({key})/AirVinyl.Actions.Rate")]
        public IHttpActionResult Rate([FromODataUri] int key, ODataActionParameters parameters)
        {
            // get the RecordStore
            var recordStore = _ctx.RecordStores
              .FirstOrDefault(p => p.RecordStoreId == key);

            if (recordStore == null)
            {
                return NotFound();
            }

            // from the param dictionary, get the rating & personid
            int rating;
            int personId;
            object outputFromDictionary;

            if (!parameters.TryGetValue("rating", out outputFromDictionary))
            {
                return NotFound();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out rating))
            {
                return NotFound();
            }

            if (!parameters.TryGetValue("personId", out outputFromDictionary))
            {
                return NotFound();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out personId))
            {
                return NotFound();
            }

            // the person must exist
            var person = _ctx.People
            .FirstOrDefault(p => p.PersonId == personId);

            if (person == null)
            {
                return NotFound();
            }

            // everything checks out, add the rating
            recordStore.Ratings.Add(new Rating() { RatedBy = person, Value = rating });

            // save changes 
            if (_ctx.SaveChanges() > -1)
            {
                // return true
                return this.CreateOKHttpActionResult(true);
            }
            else
            {
                // Something went wrong - we expect our 
                // action to return false in that case.  
                // The request is still successful, false
                // is a valid response
                return this.CreateOKHttpActionResult(false);
            }
        }

        [HttpPost]
        [ODataRoute("RecordStores/AirVinyl.Actions.RemoveRatings")]
        public IHttpActionResult RemoveRatings(ODataActionParameters parameters)
        { 
            // from the param dictionary, get the personid
            int personId;
            object outputFromDictionary;

            if (!parameters.TryGetValue("personId", out outputFromDictionary))
            {
                return NotFound();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out personId))
            {
                return NotFound();
            }

            // get the RecordStores that were rated by the person with personId
            var recordStoresRatedByCurrentPerson = _ctx.RecordStores
                .Include("Ratings").Include("Ratings.RatedBy")
                .Where(p => p.Ratings.Any(r => r.RatedBy.PersonId == personId)).ToList();

            // remove those ratings
            foreach (var store in recordStoresRatedByCurrentPerson)
            {
                // get the ratings by the current person
                var ratingsByCurrentPerson = store.Ratings
                    .Where(r => r.RatedBy.PersonId == personId).ToList();

                for (int i = 0; i < ratingsByCurrentPerson.Count(); i++)
                {
                    store.Ratings.Remove(ratingsByCurrentPerson[i]);
                }
            }

            // save changes 
            if (_ctx.SaveChanges() > -1)
            {
                // return true
                return this.CreateOKHttpActionResult(true);
            }
            else
            {
                // Something went wrong - we expect our 
                // action to return false in that case.  
                // The request is still successful, false
                // is a valid response
                return this.CreateOKHttpActionResult(false);
            }
        }

        [HttpPost]
        [ODataRoute("RemoveRecordStoreRatings")]
        public IHttpActionResult RemoveRecordStoreRatings(ODataActionParameters parameters)
        {
            // from the param dictionary, get the personid
            int personId;
            object outputFromDictionary;

            if (!parameters.TryGetValue("personId", out outputFromDictionary))
            {
                return NotFound();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out personId))
            {
                return NotFound();
            }

            // get the RecordStores that were rated by the person with personId
            var recordStoresRatedByCurrentPerson = _ctx.RecordStores
                .Include("Ratings").Include("Ratings.RatedBy")
                .Where(p => p.Ratings.Any(r => r.RatedBy.PersonId == personId)).ToList();

            // remove those ratings
            foreach (var store in recordStoresRatedByCurrentPerson)
            {
                // get the ratings by the current person
                var ratingsByCurrentPerson = store.Ratings.Where(r => r.RatedBy.PersonId == personId).ToList();
                for (int i = 0; i < ratingsByCurrentPerson.Count(); i++)
                {
                    store.Ratings.Remove(ratingsByCurrentPerson[i]);
                }
            }

            // save changes 
            if (_ctx.SaveChanges() > -1)
            {
                // return no content
                return StatusCode(HttpStatusCode.NoContent);
            }
            else
            {
                // something went wrong
                return StatusCode(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("RecordStores/AirVinyl.Model.SpecializedRecordStore")]
        public IHttpActionResult GetSpecializedRecordStores()
        {
            //var specializedStores = _ctx.RecordStores.Where(r => r is SpecializedRecordStore);
            //return Ok(specializedStores);

            // projection, required for filtering
            var specializedStores = _ctx.RecordStores.Where(r => r is SpecializedRecordStore);
            return Ok(specializedStores.Select(s => s as SpecializedRecordStore));
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("RecordStores({key})/AirVinyl.Model.SpecializedRecordStore")]
        public IHttpActionResult GetSpecializedRecordStore([FromODataUri] int key)
        {
            var specializedStores = _ctx.RecordStores
                .Where(r => r.RecordStoreId == key && r is SpecializedRecordStore);

            if (!specializedStores.Any())
            {
                return NotFound();
            }

            // return the result
            // return Ok(specializedStores.Single());

            // If you want to enable queries on this, you should return
            // an IQueryable result.  This should be used in combination with the
            // EnableQuery attribute - if not, this will fail.
            return Ok(SingleResult.Create(
                specializedStores.Select(s => s as SpecializedRecordStore)));            
        }

        protected override void Dispose(bool disposing)
        {
            // dispose the context
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }
}
