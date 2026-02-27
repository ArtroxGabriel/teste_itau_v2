# Itau Corretora - Sistema de Compra Programada de Ações (Top Five)

Este projeto é uma implementação do desafio técnico para o Sistema de Compra Programada de Ações da Itaú Corretora. O sistema permite que clientes invistam automaticamente em uma carteira recomendada ("Top Five") de forma recorrente.

## 🚀 Tecnologias Utilizadas

- **Linguagem**: C# (.NET 8.0)
- **Banco de Dados**: MySQL 8.0
- **Mensageria**: Apache Kafka
- **Gerenciador de Ambiente**: `mise`
- **Arquitetura**: Clean Architecture (DDD Pattern)
- **Documentação API**: Swagger/OpenAPI

## 🏛️ Arquitetura

O projeto segue os princípios de **Clean Architecture**, dividido nas seguintes camadas:

- **Api**: Interface REST para clientes e administradores.
- **Application**: Orquestração de casos de uso (Motores de Compra e Rebalanceamento).
- **Domain**: Entidades de negócio, regras de domínio e interfaces core.
- **Infrastructure**: Implementações de persistência (EF Core), mensageria (Kafka) e integração com arquivos B3 (COTAHIST).

## ⚙️ Pré-requisitos

- [Mise](https://mise.jdx.dev/) (para gerenciar o .NET SDK)
- [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/)

## 🛠️ Como Executar

1. **Instalar o .NET SDK via Mise**:
   ```bash
   mise install
   ```

2. **Subir a infraestrutura (MySQL + Kafka)**:
   ```bash
   docker-compose up -d
   ```

3. **Restaurar dependências e rodar o projeto**:
   ```bash
   dotnet restore
   dotnet run --project src/ItauCompraProgramada.Api
   ```

## 📊 Funcionalidades Principais

- **Motor de Compra Programada**: Execução nos dias 5, 15 e 25 de cada mês.
- **Distribuição Proporcional**: Alocação de ativos baseada no aporte de cada cliente.
- **Rebalanceamento Automático**: Ajuste de carteira por mudança de recomendação ou desvio de proporção (>5%).
- **Cálculo de IR**:
  - IR Dedo-duro (0,005%) em todas as operações.
  - IR de 20% sobre lucro em vendas superiores a R$ 20.000/mês.
- **Integração B3**: Leitura e parse do arquivo COTAHIST.

## 📁 Estrutura de Pastas

```text
/
|-- cotacoes/                  # Arquivos COTAHIST da B3
|-- src/                       # Código-fonte
|   |-- Api/                   # Web API
|   |-- Application/           # Casos de Uso
|   |-- Domain/                # Regras de Negócio
|   |-- Infrastructure/        # Persistência e Integrações
|-- tests/                     # Testes Unitários e Integração
|-- docker-compose.yml         # Infraestrutura
|-- ItauCompraProgramada.sln   # Solution .NET
```
