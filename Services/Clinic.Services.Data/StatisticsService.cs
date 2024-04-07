namespace Clinic.Services.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Clinic.Data;
    using Clinic.Services.Data.Contracts;
    using Clinic.Web.ViewModels.Administration.Statistics;
    using Microsoft.EntityFrameworkCore;

    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext db;

        public StatisticsService(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<ICollection<StatisticClinicViewModel>> GetAllClinics()
        {
            // Gets all hospitals including the clinics inside
            var hospitals = await this.db.Hospitals
                .Include(h => h.Clincs)
                .ToListAsync();

            var viewModel = new List<StatisticClinicViewModel>();

            foreach (var hospital in hospitals)
            {
                // Converts the clinics to Statistic Clinic View Model
                var clinics = hospital.Clincs.Select(c => new StatisticClinicViewModel()
                {
                    HospitalName = hospital.Name,
                    ClinicId = c.ClinicId,
                    Name = c.Name,
                })
                .ToList();

                viewModel.AddRange(clinics);
            }

            return viewModel;
        }

        public async Task<ICollection<StatisticDoctorViewModel>> GetDoctorsInClinic(string clinicId)
        {
            // Gets all doctors with the clinicId and converts them to Statistic Doctor View Model
            return await this.db.Users.Where(u => u.ClinicId == clinicId).Select(u => new StatisticDoctorViewModel()
            {
                DoctorId = u.Id,
                DoctorEmail = u.Email,
            }).ToListAsync();
        }

        public async Task<ICollection<StatisticPatientViewModel>> GetPatientsInClinic(string clinicId)
        {
            var viewModel = new List<StatisticPatientViewModel>();

            // Gets all patient ids
            var pc = await this.db.PatientClinics.Where(pc => pc.ClinicId == clinicId).ToListAsync();

            foreach (var patientId in pc)
            {
                // Get patient by id
                var patient = await this.db.Users.FirstOrDefaultAsync(u => u.Id == patientId.PatientId);

                // Convert him to Statistic Patient View Model
                viewModel.Add(new StatisticPatientViewModel()
                {
                    PatientId = patient.Id,
                    PatientEmail = patient.Email,
                });
            }

            return viewModel;
        }

        public async Task<ICollection<StatisticDiagnosticViewModel>> GetAllDiagnostics()
        {
            var viewModel = new List<StatisticDiagnosticViewModel>();

            // Gets all diagnostics
            var diagnostics = await this.db.Diagnostics
                .OrderBy(d => d.CreatorId)
                .ToListAsync();

            if (diagnostics.Count <= 0)
            {
                return viewModel;
            }

            // Gets first doctor who made the first diagnostic
            var creator = await this.db.Users.FirstOrDefaultAsync(u => u.Id == diagnostics[0].CreatorId);

            foreach (var diagnostic in diagnostics)
            {
                // If creator is the same dont change
                if (diagnostic.CreatorId == creator.Id)
                {
                    // Convert diagnostic to Statistic Diagnostic View Model
                    viewModel.Add(new StatisticDiagnosticViewModel()
                    {
                        CreatorId = diagnostic.CreatorId,
                        CreatorName = creator.Email,
                        DiagnosticId = diagnostic.DiagnosticsId,
                        Name = diagnostic.Name,
                        Description = diagnostic.Description,
                    });
                }
                // If creator is another get the creator from users
                else
                {
                    creator = await this.db.Users.FirstOrDefaultAsync(u => u.Id == diagnostic.CreatorId);
                    // Convert diagnostic to Statistic Diagnostic View Model
                    viewModel.Add(new StatisticDiagnosticViewModel()
                    {
                        CreatorId = diagnostic.CreatorId,
                        CreatorName = creator.Email,
                        DiagnosticId = diagnostic.DiagnosticsId,
                        Name = diagnostic.Name,
                        Description = diagnostic.Description,
                    });
                }
            }

            return viewModel;
        }

        public async Task<ICollection<StatisticDiagnosticPatientViewModel>> GetDiagnosticsForPatient(string patientId)
        {
            var viewModel = new List<StatisticDiagnosticPatientViewModel>();

            // Get patient diagnostics ids
            var pd = await this.db.PatientDiagnostics.Where(pd => pd.PatientId == patientId).ToListAsync();

            foreach (var diagnosticsId in pd)
            {
                // Get diagnostic by id
                var diagnostic = await this.db.Diagnostics.FirstOrDefaultAsync(d => d.DiagnosticsId == diagnosticsId.DiagnosticsId);
                // Get patient by id
                var patient = await this.db.Users.FirstOrDefaultAsync(u => u.Id == diagnosticsId.PatientId);

                // Covert to Statistic Diagnostic Patient View Model
                viewModel.Add(new StatisticDiagnosticPatientViewModel()
                {
                    PatientId = patient.Id,
                    PatientName = patient.Email,
                    Name = diagnostic.Name,
                    Description = diagnostic.Description,
                    DiagnosticId = diagnostic.DiagnosticsId,
                });
            }

            return viewModel;
        }

        public async Task<ICollection<StatisticDiagnosticViewModel>> GetDiagnosticsFromDoctor(string doctorId)
        {
            // Get doctor by id
            var doctor = await this.db.Users.FirstOrDefaultAsync(u => u.Id == doctorId);

            // Convert diagnostic to Statistic Diagnostic View Model for the given doctor
            return await this.db.Diagnostics.Where(d => d.CreatorId == doctorId).Select(d => new StatisticDiagnosticViewModel()
            {
                CreatorName = doctor.Email,
                CreatorId = d.CreatorId,
                Name = d.Name,
                Description = d.Description,
                DiagnosticId = d.DiagnosticsId,
            }).ToListAsync();
        }

        public async Task<ICollection<StatisticPatientViewModel>> GetPatientsWithDiagnostics()
        {
            var viewModel = new List<StatisticPatientViewModel>();

            // Get patients ids
            var pds = await this.db.PatientDiagnostics.OrderBy(pd => pd.PatientId).ToListAsync();

            if (pds.Count <= 0)
            {
                return viewModel;
            }

            // Get patient by id
            var patient = await this.db.Users.FirstOrDefaultAsync(u => u.Id == pds.First().PatientId);
            // Convert patient to Statistic Patient View Model
            viewModel.Add(new StatisticPatientViewModel()
            {
                PatientId = patient.Id,
                PatientEmail = patient.Email,
            });

            // Add every other patient to the view model
            foreach (var pd in pds)
            {
                if (pd.PatientId != patient.Id)
                {
                    patient = await this.db.Users.FirstOrDefaultAsync(u => u.Id == pd.PatientId);
                    viewModel.Add(new StatisticPatientViewModel()
                    {
                        PatientId = patient.Id,
                        PatientEmail = patient.Email,
                    });
                }
            }

            return viewModel;
        }
    }
}
