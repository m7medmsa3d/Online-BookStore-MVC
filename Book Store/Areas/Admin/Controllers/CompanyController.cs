using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models.ViewModels;
using BookStore.Models;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Book_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
       


        public CompanyController(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
          



        }
        public IActionResult Index()
        {
            List<Company> companyList = _unitOfWork.Company.GetAll().ToList();
            return View(companyList);
        }
        public IActionResult Upsert(int? id)
        {
       
            if (id == null || id == 0)
            {
                return View(new Company());
            }
            else
            {
                Company companyobj = _unitOfWork.Company.Get(u => u.id == id);
                return View(companyobj);
            }

        }
        [HttpPost]
        public IActionResult Upsert(Company companyobj)
        {


            if (ModelState.IsValid)
            {
                
                if (companyobj.id == 0)
                {
                    _unitOfWork.Company.Add(companyobj);
                }
                else
                {
                    _unitOfWork.Company.Update(companyobj);
                }


                _unitOfWork.Save();
                TempData["success"] = "Company add successfully";

                return RedirectToAction(nameof(Index));

            }
            else
            {
              
                return View(companyobj);
            }


        }


        #region API CALL
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = objCompanyList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company companydeleted = _unitOfWork.Company.Get(u => u.id == id);
            if (companydeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            else
            {
             _unitOfWork.Company.Remove(companydeleted);
                _unitOfWork.Save();
                return Json(new { success = true, message = "Delete successfully" });
            }
        }

        #endregion



    }
}
