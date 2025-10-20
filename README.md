# HubNewsCollection API

## Visão geral
A HubNewsCollection é uma API REST construída com ASP.NET Core 8.0 que agrega notícias de negócios obtidas via [MediaStack](https://mediastack.com/). O serviço mantém um cache em memória das matérias retornadas pela API externa e permite sincronizar, listar, atualizar e excluir artigos.
https://hubnewsapi-grfgdpgrbtd4g3cj.brazilsouth-01.azurewebsites.net/swagger/index.html


## Requisitos
- .NET SDK 8.0 ou superior
- Acesso à internet para consumir o endpoint do MediaStack (chave de acesso configurada em `FetchApiNewsService`)

## Como executar localmente
1. **Restaure as dependências**
   ```bash
   dotnet restore
   ```
2. **Execute a aplicação**
   ```bash
   dotnet run
   ```
3. A API ficará disponível em `https://localhost:7066` (HTTPS) e `http://localhost:5066` (HTTP) por padrão.
4. No perfil de desenvolvimento o Swagger UI pode ser acessado em `https://localhost:7066/swagger`.

## Serviços principais
- `FetchApiNewsService`: realiza a chamada HTTP ao MediaStack.
- `HubNewsService`: armazena os artigos em memória e controla as operações de sincronização, atualização e exclusão.

## Endpoints
A rota base do controlador é `api/HubNews`.

### Sincronizar notícias
`POST /api/HubNews/admin/notices`

Dispara a sincronização com o MediaStack e inclui no cache todos os artigos ainda não cadastrados (deduplicação por URL). Não requer corpo na requisição.

**Respostas**
- `201 Created` – Sincronização realizada com sucesso.

### Listar feed
`GET /api/HubNews/Feed`

Retorna a lista de artigos ordenada por `published_at` (mais recentes primeiro).

**Respostas**
- `200 OK` – Retorna um array de objetos `Articles`:
  ```json
  [
    {
      "id": "f6d4f3e1-4f1c-4d0a-8b4e-a8d56af33e11",
      "author": "...",
      "title": "...",
      "description": "...",
      "url": "https://exemplo.com/noticia",
      "image": "https://exemplo.com/imagem.jpg",
      "category": "business",
      "source": "mediastack",
      "published_at": "2024-01-01T12:00:00Z"
    }
  ]
  ```

### Atualizar artigo
`PUT /api/HubNews/admin/articles/{id}`

Permite alterar o título e/ou URL de um artigo existente.

**Body (JSON)**
```json
{
  "title": "Novo título opcional",
  "url": "https://nova-url-opcional.com"
}
```

Pelo menos um dos campos (`title` ou `url`) deve ser enviado. Caso a URL seja alterada, a API garante que ela não esteja sendo usada por outro artigo.

**Respostas**
- `200 OK` – Retorna o objeto `Articles` atualizado.
- `400 Bad Request` – Nenhum campo enviado para atualização.
- `404 Not Found` – Artigo não localizado ou tentativa de usar uma URL já cadastrada.

### Excluir artigo
`DELETE /api/HubNews/admin/articles/{id}`

Remove um artigo do cache em memória.

**Respostas**
- `204 No Content` – Exclusão realizada.
- `404 Not Found` – Artigo não localizado.

## Estrutura de dados
Os artigos seguem o modelo definido em `Domain/Model/Articles.cs`:

| Campo         | Tipo      | Descrição                                           |
|---------------|-----------|-----------------------------------------------------|
| `id`          | `Guid`    | Identificador único gerado pela API.                |
| `author`      | `string?` | Autor da matéria (quando informado).               |
| `title`       | `string`  | Título da notícia.                                  |
| `description` | `string`  | Resumo da notícia.                                  |
| `url`         | `string`  | URL original do artigo (usada para deduplicação).   |
| `image`       | `string?` | URL da imagem principal.                            |
| `category`    | `string`  | Categoria atribuída ao artigo (padrão `business`).  |
| `source`      | `string?` | Fonte da notícia.                                   |
| `published_at`| `DateTime?` | Data/hora de publicação em UTC.                   |

## Configuração da API externa
A URL do MediaStack está definida diretamente em `Service/FetchApiNewsService.cs`. Caso precise trocar o token `access_key` ou outros parâmetros, ajuste o valor da variável `url` naquele arquivo ou utilize variáveis de ambiente/injeção de configuração conforme a necessidade.


