using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HD.Station.Authorization.Demo.Data;
using HD.Station.Authorization.Demo.Models;
using HD.Station.Authorization.Demo.Authorization;

namespace HD.Station.Authorization.Demo.Controllers
{
    [AllowAnonymous, Authorize]
    public class ContactsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ContactDbContext _context;

        public ContactsController(ContactDbContext context,
            IAuthorizationService authorizationService,
            UserManager<IdentityUser> userManager
            )
        {
            _userManager = userManager;
            _authorizationService = authorizationService;
            _context = context;
        }


        // GET: Contacts
        public List<Contact> Contacts { get; set;}
        public Contact Contact { set; get; }
        public async Task<IActionResult> Index()
        {
            var contacts = from c in _context.Contacts select c;
            var isAuthorized = User.IsInRole(Constants.ContactAdministratorsRole) ||
                                User.IsInRole(Constants.ContactManagersRole);
            var currentUserId = _userManager.GetUserId(User);

            if (!isAuthorized)
            {
                contacts = contacts.Where(c => c.Status == ContactStatus.Approved
                                            || c.OwnerID == currentUserId);
            }
            return View(await _context.Contacts.ToListAsync());
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.ContactId == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContactId,OwnerID,Name,Address,City,State,Zip,Email,Status")] Contact contact)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            contact.OwnerID = _userManager.GetUserId(User);

            // requires using ContactManager.Authorization;
            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, contact,
                                                        ContactOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var Contacts = await _context.Contacts.FirstOrDefaultAsync(
                                             m => m.ContactId == id);
            if (Contacts == null)
            {
                return NotFound();
            }
            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, Contacts,
                                                  ContactOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            //var contact = await _context.Contacts.FindAsync(id);
            //if (contact == null)
            //{
            //    return NotFound();
            //}
            return View(Contacts);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContactId,OwnerID,Name,Address,City,State,Zip,Email,Status")] Contact contact)
        {
            if (id != contact.ContactId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
            //    try
            //    {
            //        _context.Update(contact);
            //        await _context.SaveChangesAsync();
            //    }
            //    catch (DbUpdateConcurrencyException)
            //    {
            //        if (!ContactExists(contact.ContactId))
            //        {
            //            return NotFound();
            //        }
            //        else
            //        {
            //            throw;
            //        }
            //    }
            //    return RedirectToAction(nameof(Index));
            //}
            //-------------------------------------------
            if (ModelState.IsValid)
            {
                var contacts = await _context.Contacts.AsNoTracking()
                                           .FirstOrDefaultAsync(m => m.ContactId == id);

                if (contact == null)
                {
                    return NotFound();
                }

                var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                         User, contact,
                                                         ContactOperations.Update);
                if (!isAuthorized.Succeeded)
                {
                    return Forbid();
                }
                
                Contact.OwnerID = contact.OwnerID;

                _context.Attach(Contacts).State = EntityState.Modified;

                if (Contact.Status == ContactStatus.Approved)
                {
                    // If the contact is updated after approval, 
                    // and the user cannot approve,
                    // set the status back to submitted so the update can be
                    // checked and approved.
                    var canApprove = await _authorizationService.AuthorizeAsync(User,
                                            Contacts,
                                            ContactOperations.Approve);

                    if (!canApprove.Succeeded)
                    {
                        Contact.Status = ContactStatus.Submitted;
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }

                //-----------------------------------------------

                return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.ContactId == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.ContactId == id);
        }
    }
}
