﻿namespace Clinic.Web.Areas.Administration.Controllers
{
    using Clinic.Common;
    using Clinic.Web.Controllers;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize(Roles = GlobalConstants.AdministratorRoleName)]
    [Area("Administration")]
    public class AdministrationController : BaseController
    {

    }
}
