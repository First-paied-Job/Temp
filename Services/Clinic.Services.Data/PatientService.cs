namespace Clinic.Services.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Clinic.Data;
    using Clinic.Services.Data.Contracts;
    using Clinic.Web.ViewModels.Patient.Dashboard;
    using Microsoft.EntityFrameworkCore;

    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext db;

        public PatientService(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<ICollection<ClinicPatientView>> GetClinicsForPatientAsync(string patientId)
        {
            var viewmodel = new List<ClinicPatientView>();

            // Get patient and clinics id
            var pc = await this.db.PatientClinics.Where(pc => pc.PatientId == patientId).ToListAsync();
            foreach (var clinicId in pc)
            {
                // Get clinic by id
                var clinic = await this.db.Clincs.FirstOrDefaultAsync(c => c.ClinicId == clinicId.ClinicId);

                // Convert to Clinic Patient View
                viewmodel.Add(new ClinicPatientView
                {
                    ClinicId = clinicId.ClinicId,
                    Name = clinic.Name,
                });
            }

            return viewmodel;
        }

        public async Task<ICollection<DiagnosticPatientView>> GetDiagnosticForPatientInClinic(DiagnosticInClinicInputModel input)
        {
            // Get diagnostic ids for patient
            var diagnosticIdsForPatient = await this.db.PatientDiagnostics
                .Where(pd => pd.PatientId == input.PatientId)
                .Select(pd => pd.DiagnosticsId)
                .ToListAsync();

            // Convert to Diagnostic Patient View
            return this.db.Diagnostics
                .Where(d => d.ClinicId == input.ClinicId && diagnosticIdsForPatient.Contains(d.DiagnosticsId))
                .AsEnumerable()
                .Select(d =>
                {
                    var creator = this.db.Users.FirstOrDefault(u => u.Id == d.CreatorId);

                    return new DiagnosticPatientView
                    {
                        Description = d.Description,
                        NameOfDoctor = creator.Email,
                        Name = d.Name,
                    };
                })
                .ToList();
        }
    }
}
