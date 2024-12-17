using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace Book_Store.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderid)
        {
             OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u=> u.Id == orderid,includeproperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u=>u.OrderId ==  orderid,includeproperties: "product")
            };
            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderheaderfromdb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
           orderheaderfromdb.Name = OrderVM.OrderHeader.Name;
            orderheaderfromdb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderheaderfromdb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderheaderfromdb.City = OrderVM.OrderHeader.City;
            orderheaderfromdb.State = OrderVM.OrderHeader.State;
            orderheaderfromdb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderheaderfromdb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderheaderfromdb.Carrier = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderheaderfromdb);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new {orderId = orderheaderfromdb.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStauts(OrderVM.OrderHeader.Id, SD.StatusProcessing);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u=> u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate =DateOnly.FromDateTime( DateTime.Now.AddDays(30)); 
            }
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped  Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntendId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStauts(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);

            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStauts(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);

            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled  Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [ActionName("Details")]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeproperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id, includeproperties: "product");

            var domain = "https://localhost:7250/";
            var options = new Stripe.Checkout.SessionCreateOptions
            {

                
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/dtails?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
              
                Mode = "payment",
            };
            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionlineitem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionlineitem);
            }
            var service = new Stripe.Checkout.SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
            
        }
        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStauts(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
           
            return View(orderHeaderId);
        }
        #region
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objorderheaders; 

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objorderheaders = _unitOfWork.OrderHeader.GetAll(includeproperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objorderheaders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId == UserId ,includeproperties: "ApplicationUser");
            }


                switch (status)
            {
                case "pending":
                    objorderheaders = objorderheaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objorderheaders = objorderheaders.Where(u => u.OrderStatus == SD.StatusProcessing);
                    break;
                case "completed":
                    objorderheaders = objorderheaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objorderheaders = objorderheaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;



            }
            return Json(new { data = objorderheaders });
        }
        #endregion
    }
}
