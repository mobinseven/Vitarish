# Dependency Boundary Rules

## Domain

Domain contains pure business state and behavior. It should avoid concrete technology, UI, host, app-specific, and infrastructure dependencies.

## Application

Application orchestrates use cases. Commands change state. Queries read state. Application contracts define stable ports required by application logic.

## Infrastructure

Infrastructure implements application contracts using concrete technologies such as EF Core, Elasticsearch, files, queues, HTTP clients, and background job infrastructure.

## Interface

Interface contains reusable module UI and UI-facing support models/services. It should not own host routes.

## Host

Host composes routable pages, layouts, route constants, and host-owned navigation. It should not become a reusable module UI library.

## Apps

Apps contain app-specific behavior and composition. Avoid placing reusable module business behavior in app projects.

## Shared libraries

Shared libraries contain platform primitives. Avoid placing module-specific business logic in them.
