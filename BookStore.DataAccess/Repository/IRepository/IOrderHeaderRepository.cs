using BookStore.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader obj);
        void UpdateStauts(int id, string orderstatues, string? paymentstatuse = null);
        
        void UpdateStripePaymentID(int id,string sessionId, string paymentintentId);
    }
}
