using Crm.Application.Common;
using Crm.Application.Features;
using Crm.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;
    public CustomersController(ISender sender) => _sender = sender;

    [HttpGet] public Task<PagedResult<CustomerDto>> List([FromQuery] GetCustomersQuery q) => _sender.Send(q);
    [HttpGet("{id:guid}")] public Task<Customer> Get(Guid id) => _sender.Send(new GetCustomerQuery(id));

    [HttpPost] [Authorize(Policy = AuthorizationPolicies.CanManageCustomers)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerCommand cmd)
    {
        var id = await _sender.Send(cmd);
        var c = await _sender.Send(new GetCustomerQuery(id));
        return CreatedAtAction(nameof(Get), new { id }, new CustomerDto(c.Id, c.NameEn, c.NameAr, c.Sector, c.IsGovernment, c.Website));
    }

    [HttpPut("{id:guid}")] [Authorize(Policy = AuthorizationPolicies.CanManageCustomers)]
    public Task<Unit> Update(Guid id, [FromBody] UpdateCustomerCommand cmd) => _sender.Send(cmd with { Id = id });

    [HttpGet("{id:guid}/contacts")]
    public Task<IReadOnlyList<CustomerContactDto>> Contacts(Guid id) => _sender.Send(new GetCustomerContactsQuery(id));

    [HttpPost("{id:guid}/contacts")] [Authorize(Policy = AuthorizationPolicies.CanManageCustomers)]
    public async Task<ActionResult<CustomerContactDto>> AddContact(Guid id, [FromBody] CreateCustomerContactCommand cmd)
    {
        var contactId = await _sender.Send(cmd with { CustomerId = id });
        var list = await _sender.Send(new GetCustomerContactsQuery(id));
        var created = list.First(c => c.Id == contactId);
        return CreatedAtAction(nameof(Contacts), new { id }, created);
    }
}
