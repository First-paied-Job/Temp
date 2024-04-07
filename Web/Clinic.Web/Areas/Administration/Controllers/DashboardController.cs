namespace Clinic.Web.Areas.Administration.Controllers
{
    using System.Threading.Tasks;

    using Clinic.Services.Data.Contracts;
    using Clinic.Web.ViewModels.Administration.Dashboard;
    using Clinic.Web.ViewModels.Administration.Dashboard.Clinic;
    using Clinic.Web.ViewModels.Administration.Dashboard.Hospital;
    using Microsoft.AspNetCore.Mvc;

    public class DashboardController : AdministrationController
    {
        private readonly IAdministratorService administratorService;

        public DashboardController(IAdministratorService administratorService)
        {
            this.administratorService = administratorService;
        }

        public IActionResult Index()
        {
            return this.View();
        }

        // Doctors

        public IActionResult AddDoctor()
        {
            return this.View();
            // Visualise the page AddDoctor.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> AddDoctor(DoctorInputModel input)
        {
            try
            {
                await this.administratorService.AddDoctorRoleToUser(input.Email);
            }
            catch (System.Exception e)
            {
                if (e.Message == "404, Resource not found")
                {
                    this.ModelState.AddModelError("noDoctor", "There are no results recorded for the given email.");
                }
                else
                {
                    this.ModelState.AddModelError("noDoctor", e.Message);
                }
            }

            if (!this.ModelState.IsValid)
            {
                return this.View("AddDoctor");
            }

            return this.Redirect("/");
        }

        public async Task<IActionResult> List()
        {
            // Gets the view model (all doctos) from the service
            var viewModel = await this.administratorService.GetDoctorsAsync();
            return this.View(viewModel);
        }

        public async Task<IActionResult> RemoveDoctor(string userId)
        {
            await this.administratorService.RemoveDoctorRoleFromUser(userId);

            return this.Redirect("/Administration/Dashboard/List");
        }

        // Hospitals

        // Adding hospital view
        public IActionResult AddHospital()
        {
            return this.View();
        }

        // Adding hospital post
        [HttpPost]
        public async Task<IActionResult> AddHospital(HospitalInputModel input)
        {
            try
            {
                // Adding hopsital to databse
                await this.administratorService.AddHospitalAsync(input);
            }
            catch (System.Exception e)
            {
                this.ModelState.AddModelError("noDoctor", e.Message);
            }

            if (!this.ModelState.IsValid)
            {
                // Error visualisation
                return this.View("AddHospital");
            }

            // Redirect when no errors are made
            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

        public async Task<IActionResult> RemoveHospital(string hospitalId)
        {
            // Removes hospital from database with the given id
            await this.administratorService.RemoveHospitalAsync(hospitalId);

            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

        public async Task<IActionResult> HospitalList()
        {
            // Gets all hospitals from database
            var viewModel = await this.administratorService.GetHospitalsAsync();
            return this.View(viewModel);
        }

        public async Task<IActionResult> EditHospital(string hospitalId)
        {
            // Gets hospital by id
            var viewModel = await this.administratorService.GetHospitalEdit(hospitalId);

            return this.View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditHospitalPost(EditHospitalInputModel input)
        {
            // Editing hospital in databse
            await this.administratorService.EditHospitalAsync(input);

            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

        // Clinics

        // Gets hospital id from the url /Administration/Dashboard/AddClinic?hospitalId={hositalId}
        public IActionResult AddClinic(string hospitalId)
        {
            this.ViewBag.hospitalId = hospitalId;
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> AddClinic(ClinicInputModel input)
        {
            try
            {
                // Add clinic in databse
                await this.administratorService.AddClinicToHospitalAsync(input);
            }
            catch (System.Exception e)
            {
                this.ModelState.AddModelError("noClinic", e.Message);
            }

            if (!this.ModelState.IsValid)
            {
                return this.View("AddClinic");
            }

            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

        // Gets clinic id from the url /Administration/Dashboard/RemoveClinic?clinicId={clinicId}
        public async Task<IActionResult> RemoveClinic(string clinicId)
        {
            // Remove clinic in db
            await this.administratorService.RemoveClinicAsync(clinicId);

            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

        public async Task<IActionResult> ClinicList(string hospitalId)
        {
            // Gets all clinics
            var viewModel = await this.administratorService.GetClinicsInHospitalAsync(hospitalId);
            return this.View(viewModel);
        }

        public async Task<IActionResult> EditClinic(string clinicId)
        {
            var viewModel = await this.administratorService.GetClinicEdit(clinicId);

            return this.View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditClinicPost(EditClinicInputModel input)
        {
            // Edit clinic in db
            await this.administratorService.EditClinicAsync(input);

            return this.Redirect($"/Administration/Dashboard/ClinicList?hospitalId={input.HospitalId}");
        }

        public IActionResult AddDoctorToClinic(string clinicId)
        {
            this.ViewBag.clinicId = clinicId;
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> AddDoctorToClinicPost(AddDoctorToClinicInput input)
        {
            try
            {
                // Add doctor to clinic in db
                await this.administratorService.AddDoctorToClinic(input);
            }
            catch (System.Exception e)
            {
                this.ModelState.AddModelError("noClinic", e.Message);
            }

            if (!this.ModelState.IsValid)
            {
                return this.View("AddDoctorToClinic", input);
            }

            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

        public async Task<IActionResult> RemoveDoctorFromClinic(string doctorId)
        {
            // Remove doctor from db
            await this.administratorService.RemoveDoctorFromClinic(doctorId);

            return this.Redirect("/Administration/Dashboard/HospitalList");
        }

    }
}
