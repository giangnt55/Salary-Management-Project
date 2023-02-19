using Microsoft.EntityFrameworkCore;
using SalaryManagement.Application.Common.Interfaces.Persistence;
using SalaryManagement.Contracts;
using SalaryManagement.Contracts.Contracts;
using SalaryManagement.Domain.Entities;
using System.Linq.Dynamic.Core;
using Mapster;

namespace SalaryManagement.Infrastructure.Persistence.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly SalaryManagementContext _context;

        public ContractRepository(SalaryManagementContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Contract>> GetAllContractsAsync()
        {
            return await _context.Contracts.ToListAsync();
        }

        public async Task<Contract> AddContractAsync(Contract contract)
        {
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            return contract;
        }

        public async Task<Contract?> UpdateContractAsync(Contract contract)
        {
            _context.Entry(contract).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return contract;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContractExists(contract.ContractId))
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task DeleteContractAsync(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
        }

        public async Task<ContractResponse?> GetContractByIdAsync(string id)
        {
            var contract =  await _context.Contracts.Include(x => x.Employee)
                .Include(x => x.Partner)
                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null) return null;

            return contract.Adapt<ContractResponse>();
        }

        public async Task<bool> DeleteContractAsync(string contractId)
        {
             var contract = await _context.Contracts.FindAsync(contractId);

            if (contract != null)
            {
                contract.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }

        /*public async Task<PaginatedResponse<Contract>> GetContractsAsync(int pageNumber, int pageSize, string? searchKeyword, string? sortBy, bool? isDesc)
        {

         //   var contracts = _context.Contracts.ToList();
            var query = _context.Contracts
                .Include(c => c.Employee)
                .Include(c => c.Partner)
                .AsQueryable();

            // Search contracts by keyword
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(c => c.ContractId.Contains(searchKeyword)
                || c.Employee.Name.Contains(searchKeyword)
                || c.Partner.CompanyName.Contains(searchKeyword));
            }

            // Sort contracts
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy)
                {
                    case "id":
                        query = (isDesc == true) ? query.OrderByDescending(c => c.ContractId) : query.OrderBy(c => c.ContractId);
                        break;
                    case "startDate":
                        query = (isDesc == true) ? query.OrderByDescending(c => c.StartDate) : query.OrderBy(c => c.StartDate);
                        break;
                    case "endDate":
                        query = (isDesc == true) ? query.OrderByDescending(c => c.EndDate) : query.OrderBy(c => c.EndDate);
                        break;
                    default:
                        break;
                }
            }

            var totalCount = await query.CountAsync();

            // Calculate the current page and total page based on page size and total count
            if (pageNumber < 1) pageNumber = 1;
            var currentPage = pageNumber;
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            totalPages = totalPages > 0 ? totalPages : 0;

            if (currentPage < 1)
            {
                currentPage = 1;
            }
            else if (currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            var skipRow = (currentPage - 1) * pageSize;

            skipRow = (skipRow >= 0) ? skipRow : 0;

            var paginatedQuery = query.Skip(skipRow).Take(pageSize);
            var results = await paginatedQuery.ToListAsync();

            return new PaginatedResponse<Contract>
            {
                CurrentPage = currentPage,
                TotalPages = totalPages,
                ItemPerPage = pageSize,
                TotalCount = totalCount,
                Results = results
            };

        }*/

        private bool ContractExists(string id)
        {
            return _context.Contracts.Any(e => e.ContractId == id);
        }


        public async Task<PaginatedResponse<ContractResponse>> GetAllContracts(int pageNumber, int pageSize, string? sortBy, bool isDesc, string? searchKeyword)
        {
            var query = _context.Contracts.
                Select(c => new Contract
                {
                ContractId = c.ContractId,
                Job = c.Job,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Employee = c.Employee,
                Partner = c.Partner
                })
                .AsQueryable();

            // apply search filter if searchKeyword is not null or empty
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(c => c.Job.Contains(searchKeyword) || (c.Employee != null && c.Employee.Name.Contains(searchKeyword)));
            }

            // apply sorting if sortBy is not null or empty
            if (!string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderBy(sortBy, isDesc);
            }

            var totalItems = await query.CountAsync();

            if (pageNumber < 1) pageNumber = 1;

            var currentPage = pageNumber;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            totalPages = totalPages > 0 ? totalPages : 0;

            if (currentPage < 1)
            {
                currentPage = 1;
            }
            else if (currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            var skipRow = (currentPage - 1) * pageSize;

            skipRow = (skipRow >= 0) ? skipRow : 0;

            var paginatedQuery = query.Skip(skipRow).Take(pageSize);
            var results = await paginatedQuery.ToListAsync();

            var response = new PaginatedResponse<ContractResponse>
            {
                Results = results.Adapt<List<ContractResponse>>(),
                TotalCount = totalItems,
                CurrentPage = pageNumber,
                ItemPerPage= pageSize,
                TotalPages = totalPages
            };

            return response;
        }
    }

}

