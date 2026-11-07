using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.DTOs.Auth;
using Shared.DTOs.Dueno;
using Xunit;

namespace API.Tests;

public class DuenosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DuenosEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task GetAll_ConRolRecepcionista_Retorna200OkYListaDeDuenos()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var duenos = await response.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        Assert.NotNull(duenos);
        Assert.True(duenos.Count >= 2); // Datos sembrados por DbInitializer
    }

    [Fact]
    public async Task GetAll_ConRolVeterinario_Retorna200OkYListaDeDuenos()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var duenos = await response.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        Assert.NotNull(duenos);
        Assert.True(duenos.Count >= 2);
    }

    [Fact]
    public async Task GetAll_SinToken_Retorna401Unauthorized()
    {
        var response = await _client.GetAsync("/api/duenos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ConRolAdministrador_Retorna403Forbidden()
    {
        var adminToken = await ObtenerTokenAsync("admin", "Admin1234!");
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existente_ConRolRecepcionista_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var reqAll = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        reqAll.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resAll = await _client.SendAsync(reqAll);
        var duenos = await resAll.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        var carlos = duenos!.First(d => d.Dni == "34567890");

        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/duenos/{carlos.Id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var obtenido = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(obtenido);
        Assert.Equal(carlos.Id, obtenido.Id);
        Assert.Equal("34567890", obtenido.Dni);
    }

    [Fact]
    public async Task GetById_Existente_ConRolVeterinario_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");

        var reqAll = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        reqAll.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resAll = await _client.SendAsync(reqAll);
        var duenos = await resAll.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        var dueno = duenos!.First();

        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/duenos/{dueno.Id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var obtenido = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(obtenido);
        Assert.Equal(dueno.Id, obtenido.Id);
    }

    [Fact]
    public async Task GetById_SinToken_Retorna401Unauthorized()
    {
        var response = await _client.GetAsync($"/api/duenos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ConRolAdministrador_Retorna403Forbidden()
    {
        var adminToken = await ObtenerTokenAsync("admin", "Admin1234!");
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/duenos/{Guid.NewGuid()}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NoExistente_Retorna404NotFound()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var idInexistente = Guid.NewGuid();
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/duenos/{idInexistente}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DatosValidos_ConRolRecepcionista_Retorna201Created()
    {
        // Arrange (CU-02 Flujo principal con Recepcionista)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Federico",
            Apellido = "Romero",
            Dni = "42" + Random.Shared.Next(100000, 999999),
            Telefono = "11-3344-5566",
            Email = $"fede_{Guid.NewGuid():N}@test.com",
            Direccion = "Av. de Mayo 500"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(nuevo);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var creado = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal(nuevo.Nombre, creado.Nombre);
        Assert.Equal(nuevo.Dni, creado.Dni);
    }

    [Fact]
    public async Task Create_DatosValidos_ConRolVeterinario_Retorna201Created()
    {
        // Arrange (CU-02 Flujo principal con Veterinario)
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Lucía",
            Apellido = "Méndez",
            Dni = "43" + Random.Shared.Next(100000, 999999),
            Telefono = "11-8899-0011",
            Email = $"lucia_{Guid.NewGuid():N}@test.com",
            Direccion = "Belgrano 123"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(nuevo);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal(nuevo.Nombre, creado.Nombre);
        Assert.Equal(nuevo.Dni, creado.Dni);
    }

    [Fact]
    public async Task Create_ConRolAdministrador_Retorna403Forbidden()
    {
        // Arrange (CU-02 Precondiciones: Administrador no autorizado)
        var adminToken = await ObtenerTokenAsync("admin", "Admin1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Admin",
            Apellido = "Dueño",
            Dni = "44" + Random.Shared.Next(100000, 999999),
            Telefono = "11-1111-2222",
            Email = "admin.dueño@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req.Content = JsonContent.Create(nuevo);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinToken_Retorna401Unauthorized()
    {
        // Arrange (CU-02 Petición anónima)
        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Sin",
            Apellido = "Token",
            Dni = "45112233",
            Telefono = "11-0000-0000",
            Email = "anonimo@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/duenos", nuevo);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_DniDuplicado_Retorna409Conflict()
    {
        // Arrange (RN-05: DNI ya registrado por DbInitializer)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var duplicado = new DuenoCreateDTO
        {
            Nombre = "Clon",
            Apellido = "González",
            Dni = "34567890", // DNI de Carlos González sembrado
            Telefono = "11-0000-0000",
            Email = "clon@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(duplicado);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_CamposObligatoriosFaltantes_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2a)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var incompleto = new DuenoCreateDTO
        {
            Nombre = "", // Requerido vacío
            Apellido = "Gómez",
            Dni = "30112233",
            Telefono = "",
            Email = "valido@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(incompleto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DniNoNumerico_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2b)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "María",
            Apellido = "López",
            Dni = "30A12345", // Letra en DNI
            Telefono = "11-4455-6677",
            Email = "maria@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(dto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmailInvalido_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2b)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "María",
            Apellido = "López",
            Dni = "38112233",
            Telefono = "11-4455-6677",
            Email = "email-sin-arroba" // Formato inválido
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(dto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_CampoConSoloEspacios_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2c)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "   ", // Solo espacios
            Apellido = "López",
            Dni = "38112233",
            Telefono = "11-4455-6677",
            Email = "maria@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(dto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_DatosValidos_ConRolRecepcionista_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Roberto",
            Apellido = "Sánchez",
            Dni = "46" + Random.Shared.Next(100000, 999999),
            Telefono = "11-2233-4455",
            Email = $"roberto_{Guid.NewGuid():N}@test.com",
            Direccion = "San Martín 100"
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/duenos")
        {
            Content = JsonContent.Create(nuevo)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var creado = await resCreate.Content.ReadFromJsonAsync<DuenoResponseDTO>();

        var updateDto = new DuenoUpdateDTO
        {
            Nombre = "Roberto Modificado",
            Apellido = "Sánchez",
            Dni = nuevo.Dni,
            Telefono = "11-9999-8888",
            Email = nuevo.Email,
            Direccion = "San Martín 250"
        };

        var reqUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/duenos/{creado!.Id}")
        {
            Content = JsonContent.Create(updateDto)
        };
        reqUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resUpdate = await _client.SendAsync(reqUpdate);

        Assert.Equal(HttpStatusCode.OK, resUpdate.StatusCode);
        var actualizado = await resUpdate.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(actualizado);
        Assert.Equal("Roberto Modificado", actualizado.Nombre);
        Assert.Equal("11-9999-8888", actualizado.Telefono);
    }

    [Fact]
    public async Task Update_DatosValidos_ConRolVeterinario_Retorna200Ok()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Laura",
            Apellido = "Paz",
            Dni = "47" + Random.Shared.Next(100000, 999999),
            Telefono = "11-5566-7788",
            Email = $"laura_{Guid.NewGuid():N}@test.com",
            Direccion = "Mitre 400"
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/duenos")
        {
            Content = JsonContent.Create(nuevo)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var creado = await resCreate.Content.ReadFromJsonAsync<DuenoResponseDTO>();

        var nuevoDniLibre = "51" + Random.Shared.Next(100000, 999999);
        var updateDto = new DuenoUpdateDTO
        {
            Nombre = "Laura Beatriz",
            Apellido = "Paz",
            Dni = nuevoDniLibre,
            Telefono = "11-5566-0000",
            Email = nuevo.Email,
            Direccion = "Mitre 450"
        };

        var reqUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/duenos/{creado!.Id}")
        {
            Content = JsonContent.Create(updateDto)
        };
        reqUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resUpdate = await _client.SendAsync(reqUpdate);

        Assert.Equal(HttpStatusCode.OK, resUpdate.StatusCode);
        var actualizado = await resUpdate.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(actualizado);
        Assert.Equal("Laura Beatriz", actualizado.Nombre);
        Assert.Equal(nuevoDniLibre, actualizado.Dni);
    }

    [Fact]
    public async Task Update_DniDeOtroDuenoExistente_Retorna409ConflictYSinModificar()
    {
        // Arrange (BUG-02 / RN-05 en PUT /api/duenos/{id})
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dniOriginal = "52" + Random.Shared.Next(100000, 999999);
        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Martín",
            Apellido = "Silva",
            Dni = dniOriginal,
            Telefono = "11-3344-5566",
            Email = $"martin_{Guid.NewGuid():N}@test.com"
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/duenos")
        {
            Content = JsonContent.Create(nuevo)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var creado = await resCreate.Content.ReadFromJsonAsync<DuenoResponseDTO>();

        // Intentar actualizar con el DNI "34567890" que pertenece a Carlos González
        var updateDto = new DuenoUpdateDTO
        {
            Nombre = "Martín",
            Apellido = "Silva",
            Dni = "34567890",
            Telefono = "11-3344-5566",
            Email = nuevo.Email
        };

        var reqUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/duenos/{creado!.Id}")
        {
            Content = JsonContent.Create(updateDto)
        };
        reqUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resUpdate = await _client.SendAsync(reqUpdate);
        Assert.Equal(HttpStatusCode.Conflict, resUpdate.StatusCode);

        // Comprobar que el DNI original no fue modificado
        var reqGet = new HttpRequestMessage(HttpMethod.Get, $"/api/duenos/{creado.Id}");
        reqGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resGet = await _client.SendAsync(reqGet);
        var conservado = await resGet.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.Equal(dniOriginal, conservado!.Dni);
    }

    [Theory]
    [InlineData("123", "11-2233-4455", "valido@test.com")]
    [InlineData("12345678901", "11-2233-4455", "valido@test.com")]
    [InlineData("30123456", "123", "valido@test.com")]
    [InlineData("30123456", "telefono-letras", "valido@test.com")]
    [InlineData("30123456", "11-2233-4455", "email-sin-arroba")]
    public async Task Create_DniTelefonoOEmailInvalidos_Retorna400BadRequest(
        string dni, string telefono, string email)
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "Prueba",
            Apellido = "Validación",
            Dni = dni,
            Telefono = telefono,
            Email = email
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos")
        {
            Content = JsonContent.Create(dto)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "34567890", "11-4444-5555", "carlos@veterinaria.com")]
    [InlineData("Carlos", "123", "11-4444-5555", "carlos@veterinaria.com")]
    [InlineData("Carlos", "34567890", "abc-telefono", "carlos@veterinaria.com")]
    [InlineData("Carlos", "34567890", "11-4444-5555", "correo-invalido")]
    public async Task Update_DatosInvalidos_Retorna400BadRequestYSinModificar(
        string nombre, string dni, string telefono, string email)
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var reqAll = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        reqAll.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resAll = await _client.SendAsync(reqAll);
        var duenos = await resAll.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        var carlos = duenos!.First(d => d.Dni == "34567890");

        var updateDto = new DuenoUpdateDTO
        {
            Nombre = nombre,
            Apellido = carlos.Apellido,
            Dni = dni,
            Telefono = telefono,
            Email = email,
            Direccion = carlos.Direccion
        };

        var reqUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/duenos/{carlos.Id}")
        {
            Content = JsonContent.Create(updateDto)
        };
        reqUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resUpdate = await _client.SendAsync(reqUpdate);
        Assert.Equal(HttpStatusCode.BadRequest, resUpdate.StatusCode);

        var reqGet = new HttpRequestMessage(HttpMethod.Get, $"/api/duenos/{carlos.Id}");
        reqGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resGet = await _client.SendAsync(reqGet);
        var conservado = await resGet.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.Equal(carlos.Nombre, conservado!.Nombre);
        Assert.Equal(carlos.Dni, conservado.Dni);
        Assert.Equal(carlos.Telefono, conservado.Telefono);
        Assert.Equal(carlos.Email, conservado.Email);
    }

    [Fact]
    public async Task Update_SinToken_Retorna401Unauthorized()
    {
        var updateDto = new DuenoUpdateDTO
        {
            Nombre = "Intento",
            Apellido = "Anonimo",
            Dni = "30123456",
            Telefono = "11-0000-0000",
            Email = "anonimo@test.com"
        };

        var response = await _client.PutAsJsonAsync($"/api/duenos/{Guid.NewGuid()}", updateDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_ConRolAdministrador_Retorna403Forbidden()
    {
        var adminToken = await ObtenerTokenAsync("admin", "Admin1234!");
        var updateDto = new DuenoUpdateDTO
        {
            Nombre = "Intento",
            Apellido = "Admin",
            Dni = "30123456",
            Telefono = "11-0000-0000",
            Email = "admin@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/duenos/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(updateDto)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DuenoSinMascotas_ConRolRecepcionista_Retorna204NoContent()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "ParaEliminar",
            Apellido = "Recep",
            Dni = "48" + Random.Shared.Next(100000, 999999),
            Telefono = "11-1234-5678",
            Email = $"eliminar_recep_{Guid.NewGuid():N}@test.com"
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/duenos")
        {
            Content = JsonContent.Create(nuevo)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var creado = await resCreate.Content.ReadFromJsonAsync<DuenoResponseDTO>();

        var reqDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/duenos/{creado!.Id}");
        reqDelete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resDelete = await _client.SendAsync(reqDelete);

        Assert.Equal(HttpStatusCode.NoContent, resDelete.StatusCode);
    }

    [Fact]
    public async Task Delete_DuenoSinMascotas_ConRolVeterinario_Retorna204NoContent()
    {
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "ParaEliminar",
            Apellido = "Vet",
            Dni = "49" + Random.Shared.Next(100000, 999999),
            Telefono = "11-8765-4321",
            Email = $"eliminar_vet_{Guid.NewGuid():N}@test.com"
        };

        var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/duenos")
        {
            Content = JsonContent.Create(nuevo)
        };
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resCreate = await _client.SendAsync(reqCreate);
        var creado = await resCreate.Content.ReadFromJsonAsync<DuenoResponseDTO>();

        var reqDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/duenos/{creado!.Id}");
        reqDelete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resDelete = await _client.SendAsync(reqDelete);

        Assert.Equal(HttpStatusCode.NoContent, resDelete.StatusCode);
    }

    [Fact]
    public async Task Delete_SinToken_Retorna401Unauthorized()
    {
        var response = await _client.DeleteAsync($"/api/duenos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ConRolAdministrador_Retorna403Forbidden()
    {
        var adminToken = await ObtenerTokenAsync("admin", "Admin1234!");
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/duenos/{Guid.NewGuid()}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DuenoConMascotas_Retorna409Conflict()
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        // Obtener el dueño Carlos González que tiene mascotas asociadas
        var reqAll = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        reqAll.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resAll = await _client.SendAsync(reqAll);
        var duenos = await resAll.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        var carlos = duenos!.First(d => d.Dni == "34567890");

        var reqDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/duenos/{carlos.Id}");
        reqDelete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(reqDelete);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
