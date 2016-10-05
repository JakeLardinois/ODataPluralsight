using AirVinyl.API.Helpers;
using AirVinyl.DataAccessLayer;
using AirVinyl.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace AirVinyl.API.Controllers
{
    public class SingletonController : ODataController
    {
        // context
        private AirVinylDbContext _ctx = new AirVinylDbContext();

        [HttpGet]
        [ODataRoute("Tim")]
        public IHttpActionResult GetSingletonTim()
        {
            // find Tim - he's got id 6

            var personTim = _ctx.People.FirstOrDefault(p => p.PersonId == 6);
            return Ok(personTim);
        }

        [HttpGet]
        [ODataRoute("Tim/Email")]
        [ODataRoute("Tim/FirstName")]
        [ODataRoute("Tim/LastName")]
        [ODataRoute("Tim/DateOfBirth")]
        [ODataRoute("Tim/Gender")]
        public IHttpActionResult GetPropertyOfTim()
        {
            // find Tim
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == 6);
            if (person == null)
            {
                return NotFound();
            }

            var propertyToGet = Url.Request.RequestUri.Segments.Last();
            var propertyValue = person.GetValue(propertyToGet);

            if (propertyValue == null)
            {
                // null = no content
                return StatusCode(HttpStatusCode.NoContent);
            }

            return this.CreateOKHttpActionResult(propertyValue);
        }

        [HttpGet]
        [ODataRoute("Tim/Email/$value")]
        [ODataRoute("Tim/FirstName/$value")]
        [ODataRoute("Tim/LastName/$value")]
        [ODataRoute("Tim/DateOfBirth/$value")]
        [ODataRoute("Tim/Gender/$value")]
        public object GetPersonPropertyRawValue()
        {
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == 6);
            if (person == null)
            {
                return NotFound();
            }

            var propertyToGet = Url.Request.RequestUri
                .Segments[Url.Request.RequestUri.Segments.Length - 2].TrimEnd('/');
            var propertyValue = person.GetValue(propertyToGet);

            if (propertyValue == null)
            {
                // null = no content
                return StatusCode(HttpStatusCode.NoContent);
            }

            // return the raw value => ToString()
            return this.CreateOKHttpActionResult(propertyValue.ToString());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Tim/Friends")]
        public IHttpActionResult GetCollectionPropertyForTim()
        {
            // find Tim, including the requested collection (path segment)
            var person = _ctx.People.Include(Url.Request.RequestUri.Segments.Last())
                .FirstOrDefault(p => p.PersonId == 6);

            if (person == null)
            {
                return NotFound();
            }

            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();
            var collectionPropertyValue = person.GetValue(collectionPropertyToGet);

            // return the collection
            return this.CreateOKHttpActionResult(collectionPropertyValue);
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Tim/VinylRecords")]
        public IHttpActionResult GetVinylRecordsForTim()
        {
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == 6);
            if (person == null)
            {
                return NotFound();
            }

            // return the collection
            return Ok(_ctx.VinylRecords.Where(v => v.Person.PersonId == 6));
        }

        [HttpPatch]
        [ODataRoute("Tim")]
        public IHttpActionResult PartiallyUpdateTim(Delta<Person> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // find Tim
            var currentPerson = _ctx.People.FirstOrDefault(p => p.PersonId == 6);

            // apply the patch, and save the changes
            patch.Patch(currentPerson);
            _ctx.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            // dispose the context
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }

}