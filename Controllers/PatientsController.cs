using HospitalApi.Data;
using HospitalApi.DTOs;
using HospitalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApi.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly HospitalDbContext _context;

    public PatientsController(HospitalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients
            .Include(p => p.Admissions)
                .ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, $"%{search}%") ||
                EF.Functions.Like(p.LastName, $"%{search}%"));
        }

        var patients = await query
            .Select(p => new PatientDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female",
                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),
                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedType.Id,
                            Name = ba.Bed.BedType.Name,
                            Description = ba.Bed.BedType.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.Room.Id,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                    }
                }).ToList()
            })
            .ToListAsync();

        return Ok(patients);
        
    }
    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBedToPatient(
        [FromRoute] string pesel,
        [FromBody] CreateBedAssignmentDto request)
    {
        if (request.To.HasValue && request.To <= request.From)
        {
            return BadRequest("Data końcowa przypisania łóżka musi być późniejsza niż data początkowa.");
        }

        var patientExists = await _context.Patients.AnyAsync(p => p.Pesel == pesel);

        if (!patientExists)
        {
            return NotFound($"Nie znaleziono pacjenta o numerze PESEL: {pesel}.");
        }

        var wardExists = await _context.Wards.AnyAsync(w => w.Name == request.Ward);

        if (!wardExists)
        {
            return NotFound($"Nie znaleziono oddziału o nazwie: {request.Ward}.");
        }

        var bedTypeExists = await _context.BedTypes.AnyAsync(bt => bt.Name == request.BedType);

        if (!bedTypeExists)
        {
            return NotFound($"Nie znaleziono typu łóżka o nazwie: {request.BedType}.");
        }

        var requestedTo = request.To ?? new DateTime(9999, 12, 31);

        var availableBed = await _context.Beds
            .Include(b => b.Room)
            .ThenInclude(r => r.Ward)
            .Include(b => b.BedType)
            .Include(b => b.BedAssignments)
            .Where(b =>
                b.BedType.Name == request.BedType &&
                b.Room.Ward.Name == request.Ward)
            .FirstOrDefaultAsync(b =>
                !b.BedAssignments.Any(ba =>
                    ba.From < requestedTo &&
                    (ba.To == null || ba.To > request.From)));

        if (availableBed is null)
        {
            return NotFound(
                $"Brak wolnego łóżka typu '{request.BedType}' na oddziale '{request.Ward}' w podanym okresie.");
        }

        var bedAssignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = availableBed.Id,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(bedAssignment);
        await _context.SaveChangesAsync();

        return Created($"/api/patients/{pesel}/bedassignments/{bedAssignment.Id}", new
        {
            message = "Łóżko zostało przypisane pacjentowi.",
            assignmentId = bedAssignment.Id,
            patientPesel = pesel,
            bed = new
            {
                id = availableBed.Id,
                type = availableBed.BedType.Name,
                room = availableBed.Room.Id,
                ward = availableBed.Room.Ward.Name
            },
            from = bedAssignment.From,
            to = bedAssignment.To
        });
    }
}