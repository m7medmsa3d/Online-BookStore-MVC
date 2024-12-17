using BookStore.DataAccess.Repository;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace Book_Store.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _EmailSender;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender EmailSender)
        {
            this._unitOfWork = unitOfWork;
            this._EmailSender = EmailSender;
        }
        public IActionResult Index()
        {
            var ClaimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == UserId, includeproperties: "Product"),
                orderHeader = new()
            };
            IEnumerable<ProductImages> productImages = _unitOfWork.ProductImage.GetAll();
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(u=> u.ProductId == cart.ProductId).ToList();   
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.orderHeader.OrderTotal += (cart.Count * cart.Price);
            }
            return View(ShoppingCartVM);
        }
        
			public IActionResult Summary()
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				
				ShoppingCartVM = new()
				{
					ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeproperties: "Product"),
					orderHeader = new(),

				};

				

				ShoppingCartVM.orderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
            
            
				ShoppingCartVM.orderHeader.Name = ShoppingCartVM.orderHeader.ApplicationUser.Name;
				ShoppingCartVM.orderHeader.PhoneNumber = ShoppingCartVM.orderHeader.ApplicationUser.PhoneNumber;
				ShoppingCartVM.orderHeader.StreetAddress = ShoppingCartVM.orderHeader.ApplicationUser.StreetAddress;
				ShoppingCartVM.orderHeader.City = ShoppingCartVM.orderHeader.ApplicationUser.City;
				ShoppingCartVM.orderHeader.State = ShoppingCartVM.orderHeader.ApplicationUser.State;
				ShoppingCartVM.orderHeader.PostalCode = ShoppingCartVM.orderHeader.ApplicationUser.PostalCode;
			

			

				
				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					cart.Price = GetPriceBasedOnQuantity(cart);
					ShoppingCartVM.orderHeader.OrderTotal += (cart.Price * cart.Count);
				}

				return View(ShoppingCartVM);

			}
        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPost()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			
			ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeproperties: "Product");
            ShoppingCartVM.orderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.orderHeader.ApplicationUserId= userId;
             ApplicationUser applicationuser= _unitOfWork.ApplicationUser.Get(u => u.Id == userId); 

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.orderHeader.OrderTotal += (cart.Price * cart.Count);
			}

            if(applicationuser.CompanyId.GetValueOrDefault() == 0)
            {
				ShoppingCartVM.orderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.orderHeader.OrderStatus = SD.StatusPending;
			}
            else
            {
				ShoppingCartVM.orderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.orderHeader.OrderStatus = SD.StatusApproved;
			}
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.orderHeader);
            _unitOfWork.Save();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderId = ShoppingCartVM.orderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count

                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

			if (applicationuser.CompanyId.GetValueOrDefault() == 0)
			{
                var domain = "https://localhost:7250/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    
                    SuccessUrl = domain+$"Customer/Cart/OrderConfirmation?id={ShoppingCartVM.orderHeader.Id}",
                    CancelUrl = domain+"Customer/Cart/index",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                  
                    Mode = "payment",
                };
                foreach (var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionlineitem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                         Quantity = item.Count
                    };
                    options.LineItems.Add(sessionlineitem);
                }
                var service = new Stripe.Checkout.SessionService();
                Session session= service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.orderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
			return RedirectToAction(nameof(OrderConfirmation), new {id = ShoppingCartVM.orderHeader.Id});

		}

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u=> u.Id == id,includeproperties:"ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStauts(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
                HttpContext.Session.Clear();
            }
            _EmailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Book Store",
                $"<p>New Order Created {orderHeader.Id}</p>");
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u=> u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
           _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }


		public IActionResult Plus(int CartId)
        {
            var CartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == CartId);
            CartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(CartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }
        public IActionResult Minus(int CartId)
        {
            var CartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == CartId, Tracked: true);
            if (CartFromDb.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == CartFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(CartFromDb);
            }
            else
            {
                CartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(CartFromDb);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }

        public IActionResult Remove(int CartId)
        {
            var CartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == CartId,Tracked:true);

            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == CartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.ShoppingCart.Remove(CartFromDb);
          
            _unitOfWork.Save();
      
            return RedirectToAction(nameof(Index));

        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }
    }
}
