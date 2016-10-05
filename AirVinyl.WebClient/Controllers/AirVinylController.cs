using AirVinyl.Model;
using AirVinyl.WebClient.Models;
using Microsoft.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AirVinyl.WebClient.Controllers
{
    public class AirVinylController : Controller
    {
        // GET: AirVinyl
        public ActionResult Index()
        {
            // init context
            var context = new AirVinylContainer
                (new Uri("http://localhost:5810/odata"));

            // *** get People 
            //  var peopleResponse = context.People.Execute();

            // *** get People with TotalCount
            //var peopleResponse = context.People.IncludeTotalCount().Execute()
            //     as QueryOperationResponse<Person>;

            // *** get People with TotalCount and VinylRecords
            //var peopleResponse = context.People.IncludeTotalCount()
            //  .Expand(p => p.VinylRecords)
            //  .Execute()
            //   as QueryOperationResponse<Person>;

            //var peopleAsList = peopleResponse.ToList();

            //string additionalData = "Total count: " + peopleResponse.TotalCount.ToString();

            // *** get People with TotalCount, and navigate to the next page
            //var peopleResponse = context.People.IncludeTotalCount()
            //  .Expand(p => p.VinylRecords)
            //  .Execute()
            //   as QueryOperationResponse<Person>;

            //// by calling GetContinuation, we get the next page link, returned as
            //// a DataServiceQueryContinuation<T> instance which contains it

            //// we must iterate over the list to be able to call GetContinuation()
            //var peopleAsList = peopleResponse.ToList();

            //DataServiceQueryContinuation<Person> token = peopleResponse.GetContinuation();

            //// Call Execute, passing in the continuation token to get the next page
            //peopleResponse = context.Execute(token);

            //// Iterate over the list (second page this time) again, to pass through to the View
            //peopleAsList = peopleResponse.ToList();

            //string additionalData = "Total count: " + peopleResponse.TotalCount.ToString();

            // *** Filter with Where
            //var peopleResponse = context.People
            //   .Expand(p => p.VinylRecords)
            //   .Where(p => p.FirstName.EndsWith("n"));

            //var peopleAsList = peopleResponse.ToList();

            // *** Order with OrderBy(Descending)
            //var peopleResponse = context.People
            //  .Expand(p => p.VinylRecords)
            //  .Where(p => p.FirstName.EndsWith("n"))
            //  .OrderByDescending(p => p.FirstName);

            //var peopleAsList = peopleResponse.ToList();

            // *** Client-driven Paging
            //var peopleResponse = context.People
            // .Expand(p => p.VinylRecords)
            // .Where(p => p.FirstName.EndsWith("n"))
            // .OrderByDescending(p => p.FirstName)
            // .Skip(1)
            // .Take(1);

            //var peopleAsList = peopleResponse.ToList();

            // *** Select only the fields we need
            //var selectFromPeople = context.People
            //    .Select(p => new { p.FirstName, p.LastName });

            //var selectFromPeopleResponse = selectFromPeople.ToList();

            //string additionalData = "";
            //foreach (var partialPerson in selectFromPeople)
            //{
            //    additionalData += partialPerson.FirstName + " " + partialPerson.LastName + "\n"; 
            //}      

            // get one person, by key
            // var personResponse = context.People.ByKey(1).GetValue();

            // *** Create a person
            var newPerson = new Person()
            {
                FirstName = "Maggie",
                LastName = "Smith"
            };

            context.AddToPeople(newPerson);

            var responseCreate = context.SaveChanges();

            // *** Update that person

            newPerson.FirstName = "Violet";
            context.UpdateObject(newPerson);

            var responseUpdate = context.SaveChanges();
    
            // *** Delete that person

            context.DeleteObject(newPerson);
            var responseDelete = context.SaveChanges();
            
            // Load people, ordered descending by ID to ensure we load our newly-created
            // person from the API
            var peopleResponse = context.People.OrderByDescending(p => p.PersonId);
            var peopleAsList = peopleResponse.ToList();
         
            return View(new AirVinylViewModel()
            {
                People = peopleAsList,
                //Person = personResponse,
                //AdditionalData = additionalData
            });     
        }
    }
}