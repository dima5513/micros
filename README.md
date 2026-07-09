# Micros Backend
[![CI](https://github.com/dima5513/micros/actions/workflows/ci.yml/badge.svg)](https://github.com/dima5513/micros/actions/workflows/ci.yml)

Бэкенд сервиса, который следит за поисками вакансий на hh.ru и шлёт пользователю уведомления в Telegram о новых вакансиях.

## Сервисы

- **Micros.Api** — REST API, JWT через cookies, CRUD подписок и юзеров, внутренний S2S-эндпоинт.
- **Micros.HHParser** — Прасер hh.ru по подпискам, новые вакансии публикует в RabbitMQ.
- **Micros.TgBot** — Telegram-бот, отдаёт уведомления подписчикам.
- **Micros.Core** — общие примитивы (опции, RabbitMQ, Redis, контракты).

## Стек

.NET 10, PostgreSQL + EF Core, Redis, RabbitMQ, Docker Compose.

## Запуск

```bash
cp .env.example .env
docker compose --env-file=.env up -d
```

API: `http://localhost:5090`, Swagger: `/api/docs`.

