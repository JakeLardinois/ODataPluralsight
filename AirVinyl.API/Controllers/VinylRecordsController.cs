using System.Linq;
using System.Web.Http;
using System.Web.OData;
using AirVinyl.DataAccessLayer;
using System.Web.OData.Routing;

namespace AirVinyl.API.Controllers
{
    public class VinylRecordsController : ODataController
    {
        // context
        private AirVinylDbContext _ctx = new AirVinylDbContext();

        // GET odata/VinylRecords
        [HttpGet]
        [ODataRoute("VinylRecords")]
        public IHttpActionResult GetAllVinylRecords()
        {
            return Ok(_ctx.VinylRecords);
        }

        // GET odata/VinylRecords('key')
        [HttpGet]
        [ODataRoute("VinylRecords({key})")]
        public IHttpActionResult GetOneVinylRecord([FromODataUri] int key)
        {
            var vinylRecord = _ctx.VinylRecords.FirstOrDefault(p => p.VinylRecordId == key);

            if (vinylRecord == null)
            {
                return NotFound();
            }

            return Ok(vinylRecord);
        }

        protected override void Dispose(bool disposing)
        {
            // dispose the context
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }
}
