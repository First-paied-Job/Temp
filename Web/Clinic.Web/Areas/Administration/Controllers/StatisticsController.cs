namespace Clinic.Web.Areas.Administration.Controllers
{
    using System.Threading.Tasks;

    using Clinic.Services.Data.Contracts;
    using Microsoft.AspNetCore.Mvc;

    public class StatisticsController : AdministrationController
    {
        private readonly IStatisticsService statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            this.statisticsService = statisticsService;
        }

        public IActionResult Statistics()
        {
            return this.View();
        }

        public async Task<IActionResult> Clinics()
        {
            // Gets all clinics
            var viewModel = await this.statisticsService.GetAllClinics();

            return this.View(viewModel);
        }

        public async Task<IActionResult> GetDoctorsInClinic(string clinicId)
        {
            // Get all doctors in given clinic
            var viewModel = await this.statisticsService.GetDoctorsInClinic(clinicId);

            return this.View(viewModel);
        }

        public async Task<IActionResult> GetPatientsInClinic(string clinicId)
        {
            var viewModel = await this.statisticsService.GetPatientsInClinic(clinicId);

            return this.View(viewModel);
        }

        public async Task<IActionResult> Diagnostics()
        {
            // Gets all diagnostics
            var viewModel = await this.statisticsService.GetAllDiagnostics();

            return this.View(viewModel);
        }

        public async Task<IActionResult> DiagnosticsFromDoctor(string doctorId)
        {
            // Get diagnostics from doctor from db
            var viewModel = await this.statisticsService.GetDiagnosticsFromDoctor(doctorId);

            return this.View(viewModel);
        }

        public async Task<IActionResult> DiagnosticsForPatient(string patientId)
        {
            // Get diagnostics for patient by id
            var viewModel = await this.statisticsService.GetDiagnosticsForPatient(patientId);

            return this.View(viewModel);
        }

        public async Task<IActionResult> DiagnosticsPatientList()
        {
            // Get patients with diagnostics
            var viewModel = await this.statisticsService.GetPatientsWithDiagnostics();

            return this.View(viewModel);
        }
    }
}
