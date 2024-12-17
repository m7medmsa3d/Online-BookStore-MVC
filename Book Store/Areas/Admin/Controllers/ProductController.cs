using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Book_Store.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using BookStore.Models.ViewModels;
using System.IO;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using BookStore.DataAccess.Repository;
using Microsoft.AspNetCore.Hosting;

namespace Book_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public IWebHostEnvironment _webHostEnvironment { get; }


        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            this._unitOfWork = unitOfWork;
            this._webHostEnvironment = webHostEnvironment;



        }
        public IActionResult Index()
        {
            List<Product> productList = _unitOfWork.Product.GetAll(includeproperties: "Category").ToList();
            return View(productList);
        }
        public IActionResult Upsert(int? id)
        {
         
            ProductVM productVM = new ProductVM
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                }),

                Product = new Product()

            };
            if (id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeproperties: "ProductImages");
                return View(productVM);
            }

        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                _unitOfWork.Save();


                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {

                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                            Directory.CreateDirectory(finalPath);

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImages productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                        if (productVM.Product.ProductImages == null)
                            productVM.Product.ProductImages = new List<ProductImages>();
                        productVM.Product.ProductImages.Add(productImage);
                    }

                    _unitOfWork.Product.Update(productVM.Product);
                    _unitOfWork.Save();




                }


                TempData["success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVM);
            }
        }


        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath =
                                   Path.Combine(_webHostEnvironment.WebRootPath,
                                   imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }

        #region API CALL
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeproperties: "Category").ToList();
            return Json(new { data = objProductList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product productdeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productdeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
         

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);
            if (Directory.Exists(finalPath))
            {
                string[] filepaths = Directory.GetFiles(finalPath);
                foreach(string filepath in filepaths)
                {
                    System.IO.File.Delete(filepath);
                }
                Directory.Delete(finalPath);
            }
            _unitOfWork.Product.Remove(productdeleted);
              _unitOfWork.Save();

            return Json(new { success = true, message = "Delete successfully" });
            
        }

        #endregion




    }
}


