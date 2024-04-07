﻿namespace Clinic.Web.Areas.Doctor.Controllers
{
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Clinic.Services.Data.Contracts;
    using Clinic.Web.ViewModels.Doctor.Dashboard;
    using Clinic.Web.ViewModels.Doctor.Dashboard.Diagnostics;
    using Microsoft.AspNetCore.Mvc;

    public class DashboardController : DoctorController
    {
        private readonly IDoctorService doctorService;

        public DashboardController(IDoctorService doctorService)
        {
            this.doctorService = doctorService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier); // will give the user's userId

            // Get clinic for doctor
            var viewModel = await this.doctorService.GetDoctorsClinic(userId);
            return this.View(viewModel);
        }

        public IActionResult AddPatient(string clinicId)
        {
            this.ViewBag.clinicId = clinicId;
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> AddPatient(PatientInputModel input)
        {
            try
            {
                // Add patient to clininc in db
                await this.doctorService.AddPatientToClinic(input);
            }
            catch (System.Exception e)
            {
                this.ModelState.AddModelError("noClinic", e.Message);
            }

            if (!this.ModelState.IsValid)
            {
                return this.View("AddPatient", input);
            }

            return this.Redirect("/Doctor/Dashboard");
        }

        public async Task<IActionResult> List(string clinicId)
        {
            // Gets patients in clinic from db
            var viewModel = await this.doctorService.GetPatientsAsync(clinicId);
            return this.View(viewModel);
        }

        public async Task<IActionResult> RemovePatient(PatientRemoveModel model)
        {
            // Remove patient from clinic in db
            await this.doctorService.RemovePatientFromClinic(model);

            return this.Redirect("/Doctor/Dashboard");
        }

        // Diagnostics
        public IActionResult AddDiagnosticToClinic(string clinicId)
        {
            this.ViewBag.clinicId = clinicId;
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> AddDiagnosticToClinic(DiagnosticInputModel input)
        {
            try
            {
                input.CreatorId = this.User.FindFirstValue(ClaimTypes.NameIdentifier); // will give the user's userId
                // Add diagnostic in db
                await this.doctorService.AddDiagnosticAsync(input);
            }
            catch (System.Exception e)
            {
                this.ModelState.AddModelError("noClinic", e.Message);
            }

            if (!this.ModelState.IsValid)
            {
                return this.View("AddDiagnosticToClinic", input);
            }

            return this.Redirect("/Doctor/Dashboard");
        }

        public async Task<IActionResult> RemoveDiagnostic(string diagnosticId)
        {
            // Remove diagnostic from db
            await this.doctorService.RemoveDiagnosticAsync(diagnosticId);

            return this.Redirect("/Doctor/Dashboard");
        }

        public async Task<IActionResult> DiagnosticsList(string clinicId)
        {
            // Gets all diagnostics
            var viewModel = await this.doctorService.GetDiagnosticsInClinicAsync(clinicId);
            return this.View(viewModel);
        }

        public async Task<IActionResult> EditDiagnostic(string diagnosticId)
        {
            // Get diagnostic by id from db 
            var viewModel = await this.doctorService.GetDiagnosticEditAsync(diagnosticId);

            return this.View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditDiagnosticPost(EditDiagnosticInputModel input)
        {
            try
            {
                // Edit diagnostic in db
                await this.doctorService.EditDiagnosticAsync(input);
            }
            catch (System.Exception e)
            {
                this.ModelState.AddModelError("noClinic", e.Message);
            }

            if (!this.ModelState.IsValid)
            {
                return this.View("EditDiagnostic", new DiagnosticEditViewModel
                {
                    DiagnosticId = input.DiagnosticId,
                    Description = input.Description,
                    Name = input.Name,
                });
            }

            return this.Redirect("/Doctor/Dashboard");
        }

        public async Task<IActionResult> PatientDiagnosticsList(AddDiagnosticToPatientView model)
        {
            // Gets diagnostics for patient
            var viewmodel = await this.doctorService.GetPatientDiagnosticsAsync(new AvailableDiagnosticsInput { ClinicId = model.ClinicId, PatientId = model.PatientId });
            this.ViewBag.patientId = model.PatientId;
            this.ViewBag.clinicId = model.ClinicId;
            return this.View(viewmodel);
        }

        public async Task<IActionResult> AvailableDiagnosticsForPatient(AvailableDiagnosticsInput input)
        {
            // Get available diagnostic to be added to patient
            var viewmodel = await this.doctorService.GetAvailableDiagnosticsForPatient(input);
            this.ViewBag.patientId = input.PatientId;
            this.ViewBag.clinicId = input.ClinicId;
            return this.View(viewmodel);
        }

        public async Task<IActionResult> AddDiagnosticToPatient(AddDiagnosticToPatientInput input)
        {
            // Add diagnostic to patient
            await this.doctorService.AddDiagnosticToPatientAsync(input);

            return this.Redirect($"/Doctor/Dashboard/AvailableDiagnosticsForPatient?PatientId={input.PatientId}&DiagnosticId={input.DiagnosticId}&ClinicId={input.ClinicId}");
        }

        public async Task<IActionResult> RemoveDiagnosticFromPatient(RemoveDiagnosticFromPatientModel model)
        {
            // Remove diagnostic from patient
            await this.doctorService.RemoveDiagnosticFromPatientAsync(model);

            return this.Redirect($"/Doctor/Dashboard/PatientDiagnosticsList?PatientId={model.PatientId}&ClinicId={model.ClinicId}");
        }
    }
}
