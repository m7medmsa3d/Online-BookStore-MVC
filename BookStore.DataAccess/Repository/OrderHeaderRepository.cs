using Book_Store.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderHeaderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;

        }

      

        public void Update(OrderHeader obj)
        {
            _context.orderHeaders.Update(obj);
        }

        public void UpdateStauts(int id, string orderstatues, string? paymentstatuse = null)
        {
            var orderFromDb = _context.orderHeaders.FirstOrDefault(u=> u.Id == id);
             if (orderFromDb != null)
             {
                orderFromDb.OrderStatus = orderstatues;
                if(!string.IsNullOrEmpty(paymentstatuse))
                {
                    orderFromDb.PaymentStatus = paymentstatuse;
                }
             }
        }

        public void UpdateStripePaymentID(int id, string sessionId, string paymentintentId)
        {
            var orderFromDb = _context.orderHeaders.FirstOrDefault(u => u.Id == id);
            if (!string.IsNullOrEmpty(sessionId))
            {
                orderFromDb.SessionId = sessionId;
            }
            if (!string.IsNullOrEmpty(paymentintentId))
            {
                orderFromDb.PaymentIntendId = paymentintentId;
                orderFromDb.PaymentDate = DateTime.Now;
            }
        }
    }
}
