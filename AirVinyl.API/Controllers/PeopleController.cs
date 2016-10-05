using AirVinyl.API.Helpers;
using AirVinyl.DataAccessLayer;
using AirVinyl.Model;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;

namespace AirVinyl.API.Controllers
{
    [EnableCors(origins: "http://localhost:18650", headers: "*", methods: "*")]
    public class PeopleController : ODataController
    {
        // context
        private AirVinylDbContext _ctx = new AirVinylDbContext();
        
        // GET odata/People
        [EnableQuery(MaxExpansionDepth=3, MaxSkip=10, MaxTop=5, PageSize=4)]
        public IHttpActionResult Get()
        {
            return Ok(_ctx.People);
        } 

        // GET odata/People('key')
        [EnableQuery]
        public IHttpActionResult Get([FromODataUri] int key)
        {
            // queryable version
            var people = _ctx.People.Where(p => p.PersonId == key);

            if (!people.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(people));

            // non-queryable version
            //var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            //if (person == null)
            //{
            //    return NotFound();
            //}

            //return Ok(person);
        }
        
        // Getting a collection property
        [HttpGet]
        [EnableQuery]
        [ODataRoute("People({key})/Friends")]
       // [ODataRoute("People({key})/VinylRecords")]
        public IHttpActionResult GetPersonCollectionProperty([FromODataUri] int key)
        {
            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();
            var person = _ctx.People.Include(collectionPropertyToGet)
                .FirstOrDefault(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            if (!person.HasProperty(collectionPropertyToGet))
            {
                return NotFound();
            }

            var collectionPropertyValue = person.GetValue(collectionPropertyToGet);

            // return the collection
            return this.CreateOKHttpActionResult(collectionPropertyValue);
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("People({key})/VinylRecords")]
        public IHttpActionResult GetVinylRecordsForPerson([FromODataUri] int key)
        {
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
            {
                return NotFound();
            }

            // return the collection
            return Ok(_ctx.VinylRecords.Include("DynamicVinylRecordProperties")
                .Where(v => v.Person.PersonId == key));
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("People({key})/VinylRecords({vinylRecordKey})")]
        public IHttpActionResult GetVinylRecordForPerson([FromODataUri] int key, [FromODataUri] int vinylRecordKey)
        {
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
            {
                return NotFound();
            }

            // queryable, no FirstOrDefault
            var vinylRecords = _ctx.VinylRecords.Include("DynamicVinylRecordProperties")
                .Where(v => v.Person.PersonId == key && v.VinylRecordId == vinylRecordKey);

            if (!vinylRecords.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(vinylRecords));
        }

        [HttpPost]
        [ODataRoute("People({key})/VinylRecords")]
        public IHttpActionResult CreateVinylRecordForPerson([FromODataUri] int key,
            VinylRecord vinylRecord)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // does the person exist?
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
            {
                return NotFound();
            }

            // link the person to the VinylRecord (also avoids an invalid person 
            // key on the passed-in record - key from the URI wins)
            vinylRecord.Person = person;

            // add the VinylRecord
            _ctx.VinylRecords.Add(vinylRecord);
            _ctx.SaveChanges();

            // return the created VinylRecord 
            return Created(vinylRecord);
        }

        [HttpPatch]
        [ODataRoute("People({key})/VinylRecords({vinylRecordKey})")]
        public IHttpActionResult PartiallyUpdateVinylRecordForPerson([FromODataUri] int key,
            [FromODataUri] int vinylRecordKey,
            Delta<VinylRecord> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // does the person exist?
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
            {
                return NotFound();
            }

            // find a matching vinyl record  
            var currentVinylRecord = _ctx.VinylRecords.Include("DynamicVinylRecordProperties")
                .FirstOrDefault(p => p.VinylRecordId == vinylRecordKey && p.Person.PersonId == key);

            // return NotFound if the VinylRecord isn't found
            if (currentVinylRecord == null)
            {
                return NotFound();
            }

            // apply patch
            patch.Patch(currentVinylRecord);
            _ctx.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("People({key})/VinylRecords({vinylRecordKey})")]
        public IHttpActionResult DeleteVinylRecordForPerson([FromODataUri] int key,
          [FromODataUri] int vinylRecordKey)
        {
            var currentPerson = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            // find a matching vinyl record  
            var currentVinylRecord = _ctx.VinylRecords
                .FirstOrDefault(p => p.VinylRecordId == vinylRecordKey && p.Person.PersonId == key);

            if (currentVinylRecord == null)
            {
                return NotFound();
            }

            _ctx.VinylRecords.Remove(currentVinylRecord);
            _ctx.SaveChanges();

            // return No Content
            return StatusCode(HttpStatusCode.NoContent);
        }

        // GET odata/People('key')/Property
        [HttpGet]
        // id makes no sense, because we already have the key
        [ODataRoute("People({key})/Email")]
        [ODataRoute("People({key})/FirstName")]
        [ODataRoute("People({key})/LastName")]
        [ODataRoute("People({key})/DateOfBirth")]
        [ODataRoute("People({key})/Gender")]
        public IHttpActionResult GetPersonProperty([FromODataUri] int key)
        {
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
            {
                return NotFound();
            }

            var propertyToGet = Url.Request.RequestUri.Segments.Last();

            if (!person.HasProperty(propertyToGet))
            {
                return NotFound();
            }

            var propertyValue = person.GetValue(propertyToGet);

            if (propertyValue == null)
            {
                // null = no content
                return StatusCode(HttpStatusCode.NoContent);
            }
            else
            {                
                return this.CreateOKHttpActionResult(propertyValue);
            }           
        }

        // GET odata/People('key')/Property/$value
        [HttpGet]
        [ODataRoute("People({key})/Email/$value")]
        [ODataRoute("People({key})/FirstName/$value")]
        [ODataRoute("People({key})/LastName/$value")]
        [ODataRoute("People({key})/DateOfBirth/$value")]
        [ODataRoute("People({key})/Gender/$value")]
        public object GetPersonPropertyRawValue([FromODataUri] int key)
        {
            var person = _ctx.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
            {
                return NotFound();
            }

            var propertyToGet = Url.Request.RequestUri
                .Segments[Url.Request.RequestUri.Segments.Length - 2].TrimEnd('/');

            if (!person.HasProperty(propertyToGet))
            {
                return NotFound();
            }

            var propertyValue = person.GetValue(propertyToGet);

            if (propertyValue == null)
            {
                // null = no content
                return StatusCode(HttpStatusCode.NoContent);
            }
            else
            {
                // return the raw value => ToString()
                return this.CreateOKHttpActionResult(propertyValue.ToString());
            } 
        }

        // alternative: attribute routing
        // [HttpPost]
        // [ODataRoute("People")]
        // POST odata/People
        public IHttpActionResult Post(Person person)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // add the person to the People collection
            _ctx.People.Add(person);
            _ctx.SaveChanges();

            // return the created person 
            return Created(person);
        }

        // PUT odata/People('key')
        // alternative: attribute routing
        // [HttpPut]
        // [ODataRoute("People({key})")]
        // PUT is for full updates
        public IHttpActionResult Put([FromODataUri] int key, Person person)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            // PUT is for full updates: find the person
            var currentPerson = _ctx.People.FirstOrDefault(p => p.PersonId == key);

            if (currentPerson == null)
            {
                return NotFound();
            }

            // Alternative: if the person isn't found: Upsert.  This must only
            // be used if the responsibility for creating the key isn't at 
            // server-level.  In our case, we're using auto-increment fields,
            // so this isn't allowed - code is for illustration purposes only!
            //if (currentPerson == null)
            //{
            //    // the key from the URI is the key we should use
            //    person.PersonId = key;
            //    _ctx.People.Add(person);
            //    _ctx.SaveChanges();
            //    return Created(person);
            //}

            // if there's an ID property, this should be ignored. But if we try
            // to call SetValues with a different Key value, SetValues will throw an error.
            // Therefore, we set the person's ID to the key.
            person.PersonId = currentPerson.PersonId;
            _ctx.Entry(currentPerson).CurrentValues.SetValues(person);
            _ctx.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // PATCH odata/People('key')
        // alternative: attribute routing
        // [HttpPatch]
        // [ODataRoute("People({key})")]
        // PATCH is for partial updates
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Person> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // find a matching person
            var currentPerson = _ctx.People.FirstOrDefault(p => p.PersonId == key);

            if (currentPerson == null)
            {
                return NotFound();
            }

            // Alternative: if the person isn't found: Upsert.  This must only
            // be used if the responsibility for creating the key isn't at 
            // server-level.  In our case, we're using auto-increment fields,
            // so this isn't allowed - code is for illustration purposes only!
            //if (currentPerson == null)
            //{
            //    var person = new Person();
            //    person.PersonId = key;
            //    patch.Patch(person);
            //    _ctx.People.Add(person);
            //    _ctx.SaveChanges();
            //    return Created(person);
            //}

            // apply the changeset to the matching person
            patch.Patch(currentPerson);
            _ctx.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE odata/People('key')
        // alternative: attribute routing
        // [HttpDelete]
        // [ODataRoute("People({key})")]
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentPerson = _ctx.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            // this person might be another person's friend, we
            // need to this person from their friend collections
            var peopleWithCurrentPersonAsFriend =
                _ctx.People.Include("Friends")
                .Where(p => p.Friends.Select(f => f.PersonId).AsQueryable().Contains(key));                
                        
            foreach (var person in peopleWithCurrentPersonAsFriend.ToList())
            {
                person.Friends.Remove(currentPerson);
            }
   
            _ctx.People.Remove(currentPerson);
            _ctx.SaveChanges();

            // return No Content
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST odata/People('key')/Friends/$ref
        [HttpPost]
        [ODataRoute("People({key})/Friends/$ref")]
        public IHttpActionResult CreateLinkToFriend([FromODataUri] int key, [FromBody] Uri link)
        {
            // get the current person, including friends as we need to check those
            var currentPerson = _ctx.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            // we need the key value from the passed-in link Uri
            int keyOfFriendToAdd = Request.GetKeyValue<int>(link);

            if (currentPerson.Friends.Any(item => item.PersonId == keyOfFriendToAdd))
            {
                return BadRequest(string.Format("The person with Id {0} is already linked to the person with Id {1}", 
                    key, keyOfFriendToAdd));
            }

            // find the friend
            var friendToLinkTo = _ctx.People.FirstOrDefault(p => p.PersonId == keyOfFriendToAdd);
            if (friendToLinkTo == null)
            {
                return NotFound();               
            }

            // add the friend
            currentPerson.Friends.Add(friendToLinkTo);
            _ctx.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // PUT odata/People('key')/Friends/$ref?$id={'relatedKey'}
        [HttpPut]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult UpdateLinkToFriend([FromODataUri] int key,
            [FromODataUri] int relatedKey, [FromBody] Uri link)
        {
            // get the current person, including friends as we need to check those
            var currentPerson = _ctx.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            // find the current friend
            var currentfriend = currentPerson.Friends.FirstOrDefault(item => item.PersonId == relatedKey);
            if (currentfriend == null)
            {
                return NotFound();
            }

            // check if the person isn't already linked to this friend
           
            // we need the key value from the passed-in link Uri
            int keyOfFriendToAdd = Request.GetKeyValue<int>(link);
            if (currentPerson.Friends.Any(item => item.PersonId == keyOfFriendToAdd))
            {
                return BadRequest(string.Format("The person with Id {0} is already linked to the person with Id {1}",
                    key, keyOfFriendToAdd));
            }

            // find the new friend
            var friendToLinkTo = _ctx.People.FirstOrDefault(p => p.PersonId == keyOfFriendToAdd);
            if (friendToLinkTo == null)
            {
                return NotFound();
            }
          
            // remove the old friend, add the new friend
            currentPerson.Friends.Remove(currentfriend);
            currentPerson.Friends.Add(friendToLinkTo);
            _ctx.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE odata/People('key')/Friends/$ref?$id={'relatedUriWithRelatedKey'}
        [HttpDelete]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult DeleteLinkToFriend([FromODataUri] int key, [FromODataUri] int relatedKey)
        {
            // get the current person, including friends as we need to check those
            var currentPerson = _ctx.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
            {
                return NotFound();
            }

            var friend = currentPerson.Friends.FirstOrDefault(item => item.PersonId == relatedKey);
            if (friend == null)
            {
                return NotFound();
            }

            currentPerson.Friends.Remove(friend);
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