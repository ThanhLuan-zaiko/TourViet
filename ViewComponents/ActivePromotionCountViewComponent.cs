using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TourViet.ViewComponents
{
    public class ActivePromotionCountViewComponent : ViewComponent
    {
        private readonly TourBookingDbContext _context;

        public ActivePromotionCountViewComponent(TourBookingDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var now = DateTime.UtcNow;
            var count = await _context.Promotions
                .Where(p => p.IsActive 
                    && (!p.StartAt.HasValue || p.StartAt <= now)
                    && (!p.EndAt.HasValue || p.EndAt >= now))
                .CountAsync();

            return View(count);
        }
    }
}
