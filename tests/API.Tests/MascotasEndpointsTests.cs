using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.DTOs.Auth;
using Shared.DTOs.Dueno;
using Shared.DTOs.Mascota;
using Xunit;

namespace API.Tests;

public class MascotasEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MascotasEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(string username, string password)
    {
        var loginReq = new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        response.EnsureSuccessStatusCode();

        var authData = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(authData);
        Assert.NotNull(authData.Token);
        return authData.Token;
    }

    [Fact]
    public async Task GetAll_ConRolRecepcionista_Retorna200OkYListaDeMascotas()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mascotas = await response.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        Assert.NotNull(mascotas);
        Assert.True(mascotas.Count >= 3); // Sembradas por DbInitializer
    }

    [Fact]
    public async Task GetAll_SinToken_Retorna401Unauthorized()
    {
        var response = await _client.GetAsync("/api/mascotas");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ConRolAdministrador_Retorna403Forbidden()
    {
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existente_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var reqList = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        reqList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resList = await _client.SendAsync(reqList);
        var mascotas = await resList.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        var milo = mascotas!.First(m => m.Nombre == "Milo");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/mascotas/{milo.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var obtenida = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(obtenida);
        Assert.Equal(milo.Id, obtenida.Id);
    }

    [Fact]
    public async Task GetById_NoExistente_Retorna404NotFound()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var idInexistente = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/mascotas/{idInexistente}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ConDueñoValido_Retorna201Created()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        var nuevaMascota = new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "Toby",
            Especie = "Perro",
            Raza = "Labrador",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 20.0m,
            Observaciones = "Cachorro sano"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(nuevaMascota)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal("Toby", creada.Nombre);
        Assert.True(creada.Activa);
    }

    [Fact]
    public async Task Create_CamposInvalidosOPesoCero_Retorna400BadRequest()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        var mascotaInvalida = new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "", // Vacío
            Especie = "Perro",
            Raza = "Mestizo",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 0m // Inválido
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(mascotaInvalida)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuenoInexistente_Retorna404NotFound()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var dto = new MascotaCreateDTO
        {
            DuenoId = Guid.NewGuid(),
            Nombre = "SinDueno",
            Especie = "Perro",
            Raza = "Caniche",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 6m
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_DatosValidos_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");

        // Crear una mascota para actualizar
        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        var nueva = new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "ParaActualizar",
            Especie = "Perro",
            Raza = "Beagle",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-2),
            Peso = 14m
        };
        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(nueva)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var creada = await resCreate.Content.ReadFromJsonAsync<MascotaResponseDTO>();

        var updateDto = new MascotaUpdateDTO
        {
            Nombre = "BeagleActualizado",
            Raza = "Beagle Puro",
            Sexo = "Macho",
            Peso = 15.5m,
            Observaciones = "Peso controlado"
        };

        var reqUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/mascotas/{creada!.Id}")
        {
            Content = JsonContent.Create(updateDto)
        };
        reqUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resUpdate = await _client.SendAsync(reqUpdate);
        Assert.Equal(HttpStatusCode.OK, resUpdate.StatusCode);
        var actualizada = await resUpdate.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(actualizada);
        Assert.Equal("BeagleActualizado", actualizada.Nombre);
        Assert.Equal(15.5m, actualizada.Peso);
    }

    [Fact]
    public async Task Desactivar_MascotaActiva_Retorna200OkYBajaLogica()
    {
        // MEJ-03: Baja lógica exitosa debe retornar 200 OK con confirmación y estado Inactiva
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        // Creamos una mascota exclusivamente para esta prueba
        var nueva = new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "ParaDesactivar",
            Especie = "Gato",
            Raza = "Criollo",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 3.5m
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(nueva)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var mascotaCreada = await resCreate.Content.ReadFromJsonAsync<MascotaResponseDTO>();

        // Act: Llamar al endpoint de baja lógica
        var reqDesactivar = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{mascotaCreada!.Id}/desactivar");
        reqDesactivar.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(reqDesactivar);

        // Assert: 200 OK y estado Inactiva
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var desactivada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(desactivada);
        Assert.False(desactivada.Activa);
    }

    [Fact]
    public async Task Desactivar_MascotaYaInactiva_Retorna409Conflict()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var reqList = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        reqList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resList = await _client.SendAsync(reqList);
        var mascotas = await resList.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        var rocky = mascotas!.First(m => m.Nombre == "Rocky" && !m.Activa);

        // Act: Intentar desactivar mascota previamente inactiva
        var reqDesactivar = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{rocky.Id}/desactivar");
        reqDesactivar.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(reqDesactivar);

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // CU-07: Pruebas de Reasignación de Dueño
    [Fact]
    public async Task ReasignarDueno_ConDatosValidos_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var duenoActual = duenos![0];
        var nuevoDueno = duenos[1];

        // Crear una mascota para reasignar
        var nueva = new MascotaCreateDTO
        {
            DuenoId = duenoActual.Id,
            Nombre = "ParaReasignar",
            Especie = "Perro",
            Raza = "Golden",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-2),
            Peso = 28m
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(nueva)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var mascotaCreada = await resCreate.Content.ReadFromJsonAsync<MascotaResponseDTO>();

        // Act: Reasignar al nuevo dueño
        var reasignarDto = new ReasignarDuenoDTO
        {
            NuevoDuenoId = nuevoDueno.Id
        };
        var reqReasignar = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{mascotaCreada!.Id}/reasignar-dueno")
        {
            Content = JsonContent.Create(reasignarDto)
        };
        reqReasignar.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(reqReasignar);

        // Assert: 200 OK con nuevo dueño asociado
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reasignada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(reasignada);
        Assert.Equal(nuevoDueno.Id, reasignada.DuenoId);
        Assert.Equal($"{nuevoDueno.Nombre} {nuevoDueno.Apellido}", reasignada.NombreDueno);
    }

    [Fact]
    public async Task ReasignarDueno_MismoDuenoActual_Retorna400BadRequest()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var reqList = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        reqList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resList = await _client.SendAsync(reqList);
        var mascotas = await resList.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        // Reasignar al mismo dueño actual
        var dto = new ReasignarDuenoDTO
        {
            NuevoDuenoId = milo.DuenoId
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{milo.Id}/reasignar-dueno")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReasignarDueno_NuevoDuenoInexistente_Retorna404NotFound()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var reqList = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        reqList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resList = await _client.SendAsync(reqList);
        var mascotas = await resList.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new ReasignarDuenoDTO
        {
            NuevoDuenoId = Guid.NewGuid() // No existe
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{milo.Id}/reasignar-dueno")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReasignarDueno_MascotaInactiva_Retorna409Conflict()
    {
        // A1: Reasignación sobre mascota inactiva (Rocky) -> 409 Conflict
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var reqList = new HttpRequestMessage(HttpMethod.Get, "/api/mascotas");
        reqList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resList = await _client.SendAsync(reqList);
        var mascotas = await resList.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        var rocky = mascotas!.First(m => m.Nombre == "Rocky" && !m.Activa);

        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var otroDueno = duenos!.First(d => d.Id != rocky.DuenoId);

        var dto = new ReasignarDuenoDTO
        {
            NuevoDuenoId = otroDueno.Id
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{rocky.Id}/reasignar-dueno")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReasignarDueno_RolAdministrador_Retorna403Forbidden()
    {
        var tokenAdmin = await ObtenerTokenAsync("admin", "Admin1234!");
        var dto = new ReasignarDuenoDTO
        {
            NuevoDuenoId = Guid.NewGuid()
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/mascotas/{Guid.NewGuid()}/reasignar-dueno")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReasignarDueno_SinToken_Retorna401Unauthorized()
    {
        var dto = new ReasignarDuenoDTO
        {
            NuevoDuenoId = Guid.NewGuid()
        };

        var response = await _client.PatchAsJsonAsync($"/api/mascotas/{Guid.NewGuid()}/reasignar-dueno", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
