using Book_Store.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Book_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Category> objCategoryList = _unitOfWork.Category.GetAll().ToList();
            return View(objCategoryList);
        }
        public IActionResult Create()
        {

            return View();
        }
        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "The Display Order Cannot excatly match the Name");
            }
            if (obj.Name != null && obj.Name.ToLower() == "test")
            {
                ModelState.AddModelError("", "The test name is not valid");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(obj);
                _unitOfWork.Save();
                TempData["Success"] = "Category created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(obj);


        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            else
            {
                Category? CategoryFromdb = _unitOfWork.Category.Get(u => u.Id == id);
                if (CategoryFromdb == null)
                    return NotFound();
                return View(CategoryFromdb);
            }


        }
        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "The Display Order Cannot excatly match the Name");
            }
            if (obj.Name != null && obj.Name.ToLower() == "test")
            {
                ModelState.AddModelError("", "The test name is not valid");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(obj);
                _unitOfWork.Save();
                TempData["Success"] = "Category Updated successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(obj);


        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            else
            {
                Category? CategoryFromdb = _unitOfWork.Category.Get(u => u.Id == id);
                if (CategoryFromdb == null)
                    return NotFound();
                return View(CategoryFromdb);
            }


        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            Category? obj = _unitOfWork.Category.Get(u => u.Id == id);
            if (obj == null)
                return NotFound();
            _unitOfWork.Category.Remove(obj);
            _unitOfWork.Save();
            TempData["Success"] = "Category deleted successfully";
            return RedirectToAction(nameof(Index));




        }
    }
}
