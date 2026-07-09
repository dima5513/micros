# Micros Backend
[![CI](https://github.com/dima5513/micros/actions/workflows/ci.yml/badge.svg)](https://github.com/dima5513/micros/actions/workflows/ci.yml)

Бэкенд сервиса, который следит за поисками вакансий на hh.ru и шлёт пользователю уведомления в Telegram о новых вакансиях.

Как это работает: в Api заводится подписка → Scheduler по расписанию просит её спарсить → HHParser находит новые вакансии → TgBot присылает их в Telegram. Сервисы общаются через RabbitMQ.

## Сервисы

- **Micros.Api** — REST API (JWT в cookies), управляет подписками и пользователями; об изменениях сообщает событиями в RabbitMQ. Есть внутренний S2S-эндпоинт.
- **Micros.Scheduler** — планировщик на TickerQ: хранит расписание и по нему запускает парсинг каждой подписки.
- **Micros.HHParser** — по команде планировщика парсит hh.ru и публикует новые вакансии в RabbitMQ.
- **Micros.TgBot** — Telegram-бот, доставляет уведомления подписчикам.
- **Micros.Core** — общие примитивы (опции, RabbitMQ, Redis, контракты).

## Стек

.NET 10, PostgreSQL + EF Core, Redis, RabbitMQ, TickerQ, Docker Compose.

## Запуск

```bash
cp .env.template .env
docker compose --env-file=.env up -d
```

API: `http://localhost:5090`, Swagger: `/api/docs`.
