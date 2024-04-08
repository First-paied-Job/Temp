namespace Clinic.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;

    using Clinic.Common;
    using Clinic.Data;
    using Clinic.Data.Models.Hospital;
    using Clinic.Services.Data.Contracts;
    using Clinic.Web.ViewModels.Administration.Dashboard;
    using Clinic.Web.ViewModels.Administration.Dashboard.Clinic;
    using Clinic.Web.ViewModels.Administration.Dashboard.Hospital;
    using Microsoft.EntityFrameworkCore;

    public class AdministratorService : IAdministratorService
    {
        private readonly ApplicationDbContext db;
        private readonly IDoctorService doctorService;

        public AdministratorService(ApplicationDbContext db, IDoctorService doctorService)
        {
            this.db = db;
            this.doctorService = doctorService;
        }

        // Doctor
        public async Task AddDoctorRoleToUser(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException("email", "The given email is invalid!");
            }

            // Gets the user by email from the database
            var user = this.db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                throw new ArgumentNullException("email", "There is no user with the given email!");
            }

            // Gets the role by name from the database
            var doctorRole = this.db.Roles.FirstOrDefault(r => r.Name == GlobalConstants.ClinicDoctortRoleName);

            if (doctorRole == null)
            {
                throw new InvalidOperationException($"There is no role with the name \"{GlobalConstants.ClinicDoctortRoleName}\"!");
            }

            // Creating many to many relation
            var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<string>()
            {
                RoleId = doctorRole.Id,
                UserId = user.Id,
            };

            user.Roles.Add(userRole);
            this.db.Users.Update(user);

            // Adds entity to table in databse
            await this.db.UserRoles.AddAsync(userRole);

            // Save made changes
            await this.db.SaveChangesAsync();
        }

        public async Task<ICollection<DoctorViewModel>> GetDoctorsAsync()
        {
            var viewModel = new List<DoctorViewModel>();

            var doctorRole = await this.db.Roles.FirstOrDefaultAsync(r => r.Name == GlobalConstants.ClinicDoctortRoleName);
            var doctorsIds = await this.db.UserRoles.Where(ur => ur.RoleId == doctorRole.Id).ToListAsync();
            foreach (var pair in doctorsIds)
            {
                // Gets every doctor by id and converts him to DoctorViewModel
                var doctor = await this.db.Users.Where(u => u.Id == pair.UserId)
                    .Select(u => new DoctorViewModel
                    {
                        DoctorId = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                    })
                    .FirstOrDefaultAsync();

                viewModel.Add(doctor);
            }

            return viewModel;
        }

        public async Task RemoveDoctorRoleFromUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId", "The given userId is invalid!");
            }

            var user = this.db.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentNullException("userId", "There is no user with the given userId!");
            }

            var doctorRole = this.db.Roles.FirstOrDefault(r => r.Name == GlobalConstants.ClinicDoctortRoleName);

            if (doctorRole == null)
            {
                throw new InvalidOperationException($"There is no role with the name \"{GlobalConstants.ClinicDoctortRoleName}\"!");
            }

            if (user.ClinicId != null)
            {
                await this.RemoveDoctorFromClinic(userId);
            }

            // Removing relations from many to many table UserRoles
            this.db.UserRoles.Remove(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>()
            {
                RoleId = doctorRole.Id,
                UserId = user.Id,
            });

            await this.db.SaveChangesAsync();
        }

        // Hospital
        public async Task AddHospitalAsync(HospitalInputModel input)
        {
            var check = await this.db.Hospitals.FirstOrDefaultAsync(h => h.Name == input.Name);

            // Check if one exist with this name
            if (check != null)
            {
                throw new ArgumentException("There is already a Hospital with this name!");
            }

            // Creating hospital
            var hospital = new Hospital
            {
                Name = input.Name,
            };

            // Adding hopsital to databse
            await this.db.Hospitals.AddAsync(hospital);

            await this.db.SaveChangesAsync();
        }

        public async Task<ICollection<HospitalViewModel>> GetHospitalsAsync()
        {
            // Converts hospital to the HospitalViewModel
            var hospitals = await this.db.Hospitals
                .Select(h => new HospitalViewModel
                {
                    HospitalId = h.HospitalId,
                    Name = h.Name,
                    Clinics = h.Clincs.Select(c => new ClinicDTO
                    {
                        ClinicId = c.ClinicId,
                        Name = c.Name,
                    }).ToList(),
                })
                .ToListAsync();

            return hospitals;
        }

        public async Task RemoveHospitalAsync(string hospitalId)
        {

            // Removing hospital removes all entities inside it eg People, Clinics, Diagnostics
            var hospital = await this.db.Hospitals
                .Include(h => h.Clincs).ThenInclude(c => c.People)
                .Include(c => c.Clincs).ThenInclude(c => c.Diagnostics)
                .FirstOrDefaultAsync(h => h.HospitalId == hospitalId);

            if (hospital == null)
            {
                throw new ArgumentException("This hospital does not exist!");
            }

            foreach (var clinic in hospital.Clincs)
            {
                await this.CheckPatients(clinic.ClinicId);
            }

            // Removing hospital
            this.db.Hospitals.Remove(hospital);

            await this.db.SaveChangesAsync();
        }

        public async Task EditHospitalAsync(EditHospitalInputModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input", "The given input is null!");
            }

            var hospital = await this.db.Hospitals.FirstOrDefaultAsync(h => h.HospitalId == input.HospitalId);

            if (hospital == null)
            {
                throw new InvalidOperationException("No hospital found with this id!");
            }

            // Change hospital name
            hospital.Name = input.Name;

            await this.db.SaveChangesAsync();
        }

        public async Task<EditHospitalViewModel> GetHospitalEdit(string id)
        {
            var hospitalDb = await this.db.Hospitals.FirstOrDefaultAsync(h => h.HospitalId == id);

            if (hospitalDb == null)
            {
                throw new ArgumentNullException("hospitalId", "No hospital found with this id!");
            }

            // Converth hopsital to edit view model
            return new EditHospitalViewModel()
            {
               HospitalId = hospitalDb.HospitalId,
               Name = hospitalDb.Name,
            };
        }

        // Clinic
        public async Task AddClinicToHospitalAsync(ClinicInputModel input)
        {
            // Gets hospital by id
            var employer = await this.db.Hospitals.FirstOrDefaultAsync(h => h.HospitalId == input.HospitalEmployerId);
            if (employer == null)
            {
                throw new ArgumentException("No Hospital with the given name exists!");
            }

            // Check if the clinic exist
            var check = await this.db.Clincs.FirstOrDefaultAsync(c => c.Name == input.Name);
            if (check != null)
            {
                throw new ArgumentException("This clinic already exists!");
            }

            // Creating Clinic
            var clinic = new Clinic
            {
                Name = input.Name,
                HospitalEmployerId = employer.HospitalId,
                HospitalEmployer = employer,
            };

            // Adding clinic to db
            await this.db.Clincs.AddAsync(clinic);

            // Add clinic to hospital
            employer.Clincs.Add(clinic);

            await this.db.SaveChangesAsync();
        }

        public async Task<ICollection<ClinicViewModel>> GetClinicsInHospitalAsync(string hospitalId)
        {
            // Get hospital by id and includes children
            var hospital = await this.db.Hospitals
                .Include(h => h.Clincs)
                .ThenInclude(c => c.People)
                .FirstOrDefaultAsync(h => h.HospitalId == hospitalId);

            // Converting clinic to Clinic view model
            var clinics = hospital.Clincs
                .Select(c => new ClinicViewModel
                {
                    HospitalId = hospital.HospitalId,
                    ClinicId = c.ClinicId,
                    Name = c.Name,
                    Doctors = c.People
                        .Where(p => p.ClinicId == c.ClinicId)
                        .Select(p => new DoctorDTO
                        {
                            ClinicId = p.ClinicId,
                            DoctorId = p.Id,
                            Name = p.Email,
                        })
                        .ToList(),
                }).ToList();
            return clinics;
        }

        public async Task RemoveClinicAsync(string clinicId)
        {
            // Includes all children to the clinic
            var clinic = await this.db.Clincs
                .Include(c => c.People)
                .Include(c => c.Diagnostics)
                .FirstOrDefaultAsync(h => h.ClinicId == clinicId);

            if (clinic == null)
            {
                throw new ArgumentException("This clinic does not exist!");
            }

            // Get hospital parent by id
            var hospitalEmployer = await this.db.Hospitals.FirstOrDefaultAsync(h => h.HospitalId == clinic.HospitalEmployerId);

            // Remove clinic from hospital
            hospitalEmployer.Clincs.Remove(clinic);

            await this.CheckPatients(clinic.ClinicId);

            // Remove clinic from db
            this.db.Clincs.Remove(clinic);
            await this.db.SaveChangesAsync();
        }

        public async Task EditClinicAsync(EditClinicInputModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input", "The given input is null!");
            }

            // Get clinic by id
            var clinic = await this.db.Clincs.FirstOrDefaultAsync(c => c.ClinicId == input.ClinicId);

            if (clinic == null)
            {
                throw new InvalidOperationException("No clinic found with this id!");
            }

            // Update clinic name
            clinic.Name = input.Name;

            await this.db.SaveChangesAsync();
        }

        public async Task<EditClinicViewModel> GetClinicEdit(string id)
        {
            var clinicDb = await this.db.Clincs.FirstOrDefaultAsync(c => c.ClinicId == id);

            if (clinicDb == null)
            {
                throw new ArgumentNullException("clinicId", "No clinic found with this id!");
            }

            // Convert clinic to view model
            return new EditClinicViewModel
            {
                ClinicId = clinicDb.ClinicId,
                Name = clinicDb.Name,
                HospitalId = clinicDb.HospitalEmployerId,
            };
        }

        public async Task AddDoctorToClinic(AddDoctorToClinicInput input)
        {
            // Get doctor by email
            var doctor = await this.db.Users.FirstOrDefaultAsync(u => u.Email == input.Email);

            if (doctor == null)
            {
                throw new ArgumentException("The given person is not in our system.");
            }

            // Get doctor role
            var doctorRole = await this.db.Roles.FirstOrDefaultAsync(r => r.Name == GlobalConstants.ClinicDoctortRoleName);

            var checkIfDoctor = await this.db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == doctor.Id && ur.RoleId == doctorRole.Id);

            if (checkIfDoctor == null)
            {
                throw new ArgumentException("The given person is not a doctor.");
            }

            if (doctor.ClinicId != null)
            {
                throw new ArgumentException("The given doctor already is in another clinic.");
            }

            // Get clinic by id
            var clinic = await this.db.Clincs.FirstOrDefaultAsync(c => c.ClinicId == input.ClinicId);

            if (clinic == null)
            {
                throw new ArgumentException("The given clinic does not exist.");
            }

            // Add doctor to clinic
            clinic.People.Add(doctor);

            // Add clinic to doctor
            doctor.Clinic = clinic;
            doctor.ClinicId = clinic.ClinicId;
            await this.db.SaveChangesAsync();
        }

        public async Task RemoveDoctorFromClinic(string doctorId)
        {
            var doctor = await this.db.Users.FirstOrDefaultAsync(u => u.Id == doctorId);

            if (doctor == null)
            {
                throw new ArgumentException("This doctor does not exist!");
            }

            // Get the clinic by id including the people inside
            var clinic = await this.db.Clincs
                .Include(c => c.People)
                .FirstOrDefaultAsync(c => c.ClinicId == doctor.ClinicId);

            if (clinic == null)
            {
                throw new ArgumentException("This doctor is not in this clinic!");
            }

            // Removing doctor from the clinic
            doctor.ClinicId = null;
            doctor.Clinic = null;
            clinic.People.Remove(doctor);
            await this.db.SaveChangesAsync();
        }

        // Removes patient's role if in no clinics
        public async Task CheckPatients(string clinicId)
        {
            var patientClinics = await this.db.PatientClinics.Where(pc => pc.ClinicId == clinicId).ToListAsync();

            this.db.PatientClinics.RemoveRange(patientClinics);
            await this.db.SaveChangesAsync();

            foreach (var pair in patientClinics)
            {
                await this.doctorService.RemovePatientRolesAsync(pair.PatientId);
            }
        }
    }
}
