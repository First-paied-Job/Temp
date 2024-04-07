namespace Clinic.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Clinic.Common;
    using Clinic.Data;
    using Clinic.Data.Models.Hospital;
    using Clinic.Services.Data.Contracts;
    using Clinic.Web.ViewModels.Doctor.Dashboard;
    using Clinic.Web.ViewModels.Doctor.Dashboard.Diagnostics;
    using Microsoft.EntityFrameworkCore;

    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext db;

        public DoctorService(ApplicationDbContext db)
        {
            this.db = db;
        }

        // Patients
        public async Task AddPatientToClinic(PatientInputModel input)
        {
            if (string.IsNullOrEmpty(input.Email))
            {
                throw new ArgumentNullException("email", "The given email is invalid!");
            }

            // Get user by id
            var user = await this.db.Users.FirstOrDefaultAsync(u => u.Email == input.Email);

            // Get patient role by name
            var clinicRole = this.db.Roles.FirstOrDefault(r => r.Name == GlobalConstants.ClinicPatientRoleName);

            if (clinicRole == null)
            {
                throw new InvalidOperationException($"There is no role with the name \"{GlobalConstants.ClinicPatientRoleName}\"!");
            }

            // Check if user has patient role
            var roleCheck = await this.db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == clinicRole.Id);

            if (roleCheck == null)
            {
                this.db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>()
                {
                    RoleId = clinicRole.Id,
                    UserId = user.Id,
                });
            }

            // Get clinic including people inside by clinic id
            var clinic = await this.db.Clincs.Include(c => c.People).FirstOrDefaultAsync(c => c.ClinicId == input.ClinicId);

            if (clinic == null)
            {
                throw new ArgumentException("This clinic does not exist!");
            }

            // Check if patient is in clinic
            if (clinic.People.FirstOrDefault(p => p.Id == user.Id) != null)
            {
                throw new ArgumentException("The person is already registered in this clinic!");
            }

            // Add patient to clinic
            this.db.PatientClinics.Add(new PatientClinics
            {
                ClinicId = clinic.ClinicId,
                PatientId = user.Id,
            });

            await this.db.SaveChangesAsync();
        }

        public async Task<IndexViewModel> GetDoctorsClinic(string userId)
        {

            // Get doctor by id
            var user = await this.db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user.ClinicId == null)
            {
                return new IndexViewModel()
                {
                    ClinicId = null,
                    ClinicName = null,
                };
            }

            // Get clinic by Id
            var clinic = await this.db.Clincs.FirstOrDefaultAsync(c => c.ClinicId == user.ClinicId);

            return new IndexViewModel()
            {
                ClinicId = clinic.ClinicId,
                ClinicName = clinic.Name,
            };
        }

        public async Task<ICollection<PatientViewModel>> GetPatientsAsync(string clinicId)
        {
            var viewModel = new List<PatientViewModel>();

            // Get patients ids
            var patientIds = await this.db.PatientClinics.Where(pc => pc.ClinicId == clinicId).ToListAsync();
            foreach (var pair in patientIds)
            {
                // Convert to Patient View Model
                var patient = await this.db.Users.Where(u => u.Id == pair.PatientId)
                    .Select(u => new PatientViewModel
                    {
                        PatientId = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        ClinicId = pair.ClinicId,
                    })
                    .FirstOrDefaultAsync();

                viewModel.Add(patient);
            }

            return viewModel;
        }

        public async Task RemovePatientFromClinic(PatientRemoveModel model)
        {
            if (string.IsNullOrEmpty(model.PatientId))
            {
                throw new ArgumentNullException("userId", "The given userId is invalid!");
            }

            // Get patient by id
            var user = await this.db.Users.FirstOrDefaultAsync(u => u.Id == model.PatientId);

            if (user == null)
            {
                throw new ArgumentNullException("userId", "There is no user with the given userId!");
            }

            // Get clinic by id
            var clinic = await this.db.Clincs.FirstOrDefaultAsync(c => c.ClinicId == model.ClinicId);

            if (clinic == null)
            {
                throw new ArgumentNullException("clinicId", "There is no clinic with the given clinicId!");
            }

            // Check if patient is in clinic
            var check = await this.db.PatientClinics.FirstOrDefaultAsync(pc => pc.ClinicId == model.ClinicId && pc.PatientId == model.PatientId);

            if (check == null)
            {
                throw new ArgumentException("This patient is not in this clinic");
            }

            // Get clinic diagnostics ids
            var diagnosticsInClinicIds = await this.db.Diagnostics
                .Where(d => d.ClinicId == clinic.ClinicId)
                .Select(d => d.DiagnosticsId)
                .ToListAsync();

            // Get patient diagnostics ids
            var pd = await this.db.PatientDiagnostics
                .Where(pd => pd.PatientId == user.Id && diagnosticsInClinicIds.Contains(pd.DiagnosticsId))
                .ToListAsync();

            if (pd.Count > 0)
            {
                // Remove patient diagnostics
                this.db.PatientDiagnostics.RemoveRange(pd);
                await this.db.SaveChangesAsync();
            }

            // Remove patient from clinic
            this.db.PatientClinics.Remove(check);
            await this.db.SaveChangesAsync();

            // Remove patient role
            await this.RemovePatientRolesAsync(user.Id);
        }

        public async Task RemovePatientRolesAsync(string userId)
        {
            if (await this.db.PatientClinics.FirstOrDefaultAsync(pc => pc.PatientId == userId) == null)
            {
                // Remove Patient role if not in any clinics
                var patientRole = this.db.Roles.FirstOrDefault(r => r.Name == GlobalConstants.ClinicPatientRoleName);

                if (patientRole == null)
                {
                    throw new InvalidOperationException($"There is no role with the name \"{GlobalConstants.ClinicPatientRoleName}\"!");
                }

                this.db.UserRoles.Remove(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>()
                {
                    RoleId = patientRole.Id,
                    UserId = userId,
                });

                await this.db.SaveChangesAsync();
            }
        }

        // Diagnostics
        public async Task AddDiagnosticAsync(DiagnosticInputModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input", "Invalid data");
            }

            // Get clinic including diagnostics
            var clinic = await this.db.Clincs
                .Include(c => c.Diagnostics)
                .FirstOrDefaultAsync(c => c.ClinicId == input.ClinicId);

            if (clinic == null)
            {
                throw new ArgumentException("This clinic does not exist!");
            }

            // Creating diagnostic
            var diagnostic = new Diagnostics()
            {
                Name = input.Name,
                Description = input.Description,
                CreatorId = input.CreatorId,
                ClinicId = input.ClinicId,
                Clinic = clinic,
            };

            // Add diagnostic to clinic
            clinic.Diagnostics.Add(diagnostic);

            // Add diagnostic to db
            await this.db.Diagnostics.AddAsync(diagnostic);
            await this.db.SaveChangesAsync();
        }

        public async Task RemoveDiagnosticAsync(string diagnosticId)
        {
            // Get diagnostic by id
            var diagnostic = await this.db.Diagnostics.FirstOrDefaultAsync(d => d.DiagnosticsId == diagnosticId);

            if (diagnostic == null)
            {
                throw new ArgumentException("No diagnostic with this id exists!");
            }

            // Remove from db
            this.db.Diagnostics.Remove(diagnostic);
            await this.db.SaveChangesAsync();
        }

        public async Task<ICollection<DiagnosticViewModel>> GetDiagnosticsInClinicAsync(string clinicId)
        {
            // Convert diagnostics to Diagnostic View Model
            var diagnostics = await this.db.Diagnostics
                .Where(d => d.ClinicId == clinicId)
                .Select(d => new DiagnosticViewModel
                {
                    DiagnosticId = d.DiagnosticsId,
                    Name = d.Name,
                    Description = d.Description,
                })
                .ToListAsync();

            return diagnostics;
        }

        public async Task<DiagnosticEditViewModel> GetDiagnosticEditAsync(string diagnosticId)
        {
            // Get diagnostic by id
            var diagnostic = await this.db.Diagnostics.FirstOrDefaultAsync(d => d.DiagnosticsId == diagnosticId);

            if (diagnostic == null)
            {
                throw new ArgumentException("This diagnostic does not exist");
            }

            // Convert to Diagnostic Edit View Model
            return new DiagnosticEditViewModel
            {
                Description = diagnostic.Description,
                DiagnosticId = diagnosticId,
                Name = diagnostic.Name,
            };
        }

        public async Task EditDiagnosticAsync(EditDiagnosticInputModel input)
        {
            // Get diagnostic by id
            var diagnostic = await this.db.Diagnostics.FirstOrDefaultAsync(d => d.DiagnosticsId == input.DiagnosticId);

            if (diagnostic == null)
            {
                throw new ArgumentException("This diagnostic does not exist");
            }

            // Update diagnostic
            diagnostic.Name = input.Name;
            diagnostic.Description = input.Description;
            await this.db.SaveChangesAsync();
        }

        public async Task RemoveDiagnosticFromPatientAsync(RemoveDiagnosticFromPatientModel model)
        {
            // Get relation diagnostic and patient ids
            var pd = await this.db.PatientDiagnostics
                .FirstOrDefaultAsync(dp => dp.PatientId == model.PatientId && dp.DiagnosticsId == model.DiagnosticId);

            if (pd == null)
            {
                throw new ArgumentException("This patient does not have this diagnostic");
            }

            // Remove relation
            this.db.PatientDiagnostics.Remove(pd);
            await this.db.SaveChangesAsync();
        }

        public async Task AddDiagnosticToPatientAsync(AddDiagnosticToPatientInput input)
        {
            // Get diagnostic by id
            var diagnostic = await this.db.Diagnostics.FirstOrDefaultAsync(d => d.DiagnosticsId == input.DiagnosticId);

            if (diagnostic == null)
            {
                throw new ArgumentException("This diagnostic does not exist!");
            }
            // Get patient by id
            var patient = await this.db.Users.FirstOrDefaultAsync(u => u.Id == input.PatientId);

            if (patient == null)
            {
                throw new ArgumentException("This user does not exist!");
            }

            // Add diagnsotic to pateint in many to many table PatientDiagnostics
            await this.db.PatientDiagnostics.AddAsync(new PatientDiagnostics
            {
                PatientId = patient.Id,
                DiagnosticsId = diagnostic.DiagnosticsId,
                Patient = patient,
                Diagnostics = diagnostic,
            });
            await this.db.SaveChangesAsync();
        }

        public async Task<ICollection<PatientDiagnosticView>> GetPatientDiagnosticsAsync(AvailableDiagnosticsInput input)
        {
            // Get diagnostics for patient
            var diagnosticIdsForPatient = await this.db.PatientDiagnostics.Where(pc => pc.PatientId == input.PatientId).Select(pc => pc.DiagnosticsId).ToListAsync();

            // Convert to Patient Diagnostic View
            return await this.db.Diagnostics.Where(d => diagnosticIdsForPatient.Contains(d.DiagnosticsId) && d.ClinicId == input.ClinicId)
                    .Select(d => new PatientDiagnosticView
                    {
                        PatientId = input.PatientId,
                        Description = d.Description,
                        DiagnosticId = d.DiagnosticsId,
                        Name = d.Name,
                    })
                    .ToListAsync();
        }

        public async Task<ICollection<DiagnosticViewModel>> GetAvailableDiagnosticsForPatient(AvailableDiagnosticsInput input)
        {
            var viewModel = new List<DiagnosticViewModel>();

            // Get all diagnostic which are not in the users diagnsotic list already and covert to Diagnostic View Model
            var avaiableDiagnostics = await this.db.Diagnostics
                .Include(d => d.PatientDiagnostics)
                .Where(d => d.PatientDiagnostics.FirstOrDefault(pd => pd.PatientId == input.PatientId) == null && d.ClinicId == input.ClinicId)
                .Select(d => new DiagnosticViewModel
                {
                    Description = d.Description,
                    DiagnosticId = d.DiagnosticsId,
                    Name = d.Name,
                })
                .ToListAsync();

            return avaiableDiagnostics;
        }
    }
}
