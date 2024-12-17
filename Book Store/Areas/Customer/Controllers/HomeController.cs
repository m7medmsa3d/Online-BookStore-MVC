using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Book_Store.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
        
            IEnumerable<Product> productlist = _unitOfWork.Product.GetAll(includeproperties:"Category,ProductImages").ToList();
            return View(productlist);
        }
        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new ()
            {
                Product = _unitOfWork.Product.Get(u=> u.Id == productId, includeproperties: "Category,ProductImages"),
                 Count = 1,
                 ProductId = productId
            };
           
            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
           var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cart.ApplicationUserId = UserId;
            ShoppingCart cartfromdb = _unitOfWork.ShoppingCart.Get(u=>u.ApplicationUserId == UserId && u.ProductId == cart.ProductId);
            if (cartfromdb != null)
            {
                cartfromdb.Count += cart.Count;
                _unitOfWork.ShoppingCart.Update(cartfromdb);
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == UserId ).Count());
            }
            TempData["success"] = "Cart updated successfully";
           
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
