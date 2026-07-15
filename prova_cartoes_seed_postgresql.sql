-- Prova tecnica - API REST de Cartoes de Credito
-- Banco: PostgreSQL
-- Observacao: este script contem apenas a estrutura e a massa inicial para execucao da prova.
-- Os identificadores UUID sao gerados automaticamente pelo PostgreSQL via gen_random_uuid().

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DROP TABLE IF EXISTS credit_cards;
DROP TABLE IF EXISTS users;

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(120) NOT NULL,
    email VARCHAR(180) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE credit_cards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    cardholder_name VARCHAR(120) NOT NULL,
    nickname VARCHAR(80),
    brand VARCHAR(40) NOT NULL,
    first_four_digits CHAR(4) NOT NULL,
    last_four_digits CHAR(4) NOT NULL,
    expiration_date DATE NOT NULL,
    credit_limit NUMERIC(18, 2) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ,
    CONSTRAINT ck_credit_cards_first_four_digits CHECK (first_four_digits ~ '^[0-9]{4}$'),
    CONSTRAINT ck_credit_cards_last_four_digits CHECK (last_four_digits ~ '^[0-9]{4}$'),
    CONSTRAINT ck_credit_cards_status CHECK (status IN ('ACTIVE', 'BLOCKED', 'CANCELLED')),
    CONSTRAINT ck_credit_cards_credit_limit CHECK (credit_limit >= 0)
);

CREATE INDEX ix_credit_cards_user_created_at
    ON credit_cards (user_id, created_at DESC);

CREATE INDEX ix_credit_cards_user_expiration_date
    ON credit_cards (user_id, expiration_date);

INSERT INTO users (full_name, email, created_at) VALUES
('Mariana Alves', 'mariana.alves@cardcorp.test', '2026-01-10 09:00:00+00'),
('Rafael Souza',  'rafael.souza@cardcorp.test',  '2026-01-11 10:30:00+00'),
('Camila Rocha',  'camila.rocha@cardcorp.test',  '2026-01-12 14:45:00+00');

-- Mariana Alves - 12 cartoes para validar navegacao paginada com 10 itens por pagina
INSERT INTO credit_cards
(user_id, cardholder_name, nickname, brand, first_four_digits, last_four_digits, expiration_date, credit_limit, status, created_at, updated_at)
VALUES
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Principal',      'VISA',       '5321', '5336', '2028-01-31', 12000.00, 'ACTIVE',    '2026-06-30 09:10:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Viagens',        'MASTERCARD', '5412', '1002', '2028-02-29',  8500.00, 'ACTIVE',    '2026-06-29 11:20:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Corporativo',    'VISA',       '4532', '1003', '2028-03-31', 15000.00, 'BLOCKED',   '2026-06-28 15:40:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Compras Online', 'ELO',        '6505', '1004', '2028-04-30',  3000.00, 'ACTIVE',    '2026-06-27 08:05:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Assinaturas',    'VISA',       '4984', '1005', '2028-05-31',  2500.00, 'ACTIVE',    '2026-06-26 20:15:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Reserva',        'MASTERCARD', '5522', '1006', '2028-06-30',  5000.00, 'ACTIVE',    '2026-06-25 13:35:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Premium',        'AMEX',       '3714', '1007', '2028-07-31', 20000.00, 'ACTIVE',    '2026-06-24 16:25:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Digital',        'VISA',       '4012', '1008', '2028-08-31',  4000.00, 'ACTIVE',    '2026-06-23 12:00:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Beneficios',     'ELO',        '6362', '1009', '2028-09-30',  6000.00, 'CANCELLED', '2026-06-22 10:45:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Emergencia',     'MASTERCARD', '5555', '1010', '2028-10-31',  7000.00, 'ACTIVE',    '2026-06-21 09:30:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Internacional',  'VISA',       '4321', '1011', '2028-11-30', 11000.00, 'ACTIVE',    '2026-06-20 17:50:00+00', NULL),
((SELECT id FROM users WHERE email = 'mariana.alves@cardcorp.test'), 'MARIANA ALVES', 'Antigo',         'MASTERCARD', '5100', '1012', '2028-12-31',  4500.00, 'BLOCKED',   '2026-06-19 18:05:00+00', NULL);

-- Rafael Souza - 4 cartoes
INSERT INTO credit_cards
(user_id, cardholder_name, nickname, brand, first_four_digits, last_four_digits, expiration_date, credit_limit, status, created_at, updated_at)
VALUES
((SELECT id FROM users WHERE email = 'rafael.souza@cardcorp.test'), 'RAFAEL SOUZA', 'Principal', 'VISA',       '4111', '2201', '2029-01-31',  9000.00, 'ACTIVE',  '2026-06-18 14:10:00+00', NULL),
((SELECT id FROM users WHERE email = 'rafael.souza@cardcorp.test'), 'RAFAEL SOUZA', 'Empresa',   'MASTERCARD', '5454', '2202', '2029-02-28', 13000.00, 'ACTIVE',  '2026-06-17 09:35:00+00', NULL),
((SELECT id FROM users WHERE email = 'rafael.souza@cardcorp.test'), 'RAFAEL SOUZA', 'Streaming', 'ELO',        '6504', '2203', '2029-03-31',  1500.00, 'BLOCKED', '2026-06-16 21:15:00+00', NULL),
((SELECT id FROM users WHERE email = 'rafael.souza@cardcorp.test'), 'RAFAEL SOUZA', 'Backup',    'VISA',       '4012', '2204', '2029-04-30',  3000.00, 'ACTIVE',  '2026-06-15 07:50:00+00', NULL);

-- Camila Rocha - 7 cartoes
INSERT INTO credit_cards
(user_id, cardholder_name, nickname, brand, first_four_digits, last_four_digits, expiration_date, credit_limit, status, created_at, updated_at)
VALUES
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Principal',   'MASTERCARD', '5556', '3301', '2030-01-31', 10000.00, 'ACTIVE',    '2026-06-14 12:15:00+00', NULL),
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Viagens',     'VISA',       '4024', '3302', '2030-02-28',  9500.00, 'ACTIVE',    '2026-06-13 19:30:00+00', NULL),
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Online',      'ELO',        '6516', '3303', '2030-03-31',  2500.00, 'ACTIVE',    '2026-06-12 10:40:00+00', NULL),
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Mercado',     'VISA',       '4000', '3304', '2030-04-30',  4000.00, 'BLOCKED',   '2026-06-11 08:25:00+00', NULL),
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Corporativo', 'MASTERCARD', '5312', '3305', '2030-05-31', 16000.00, 'ACTIVE',    '2026-06-10 16:00:00+00', NULL),
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Reserva',     'VISA',       '4929', '3306', '2030-06-30',  5000.00, 'ACTIVE',    '2026-06-09 11:55:00+00', NULL),
((SELECT id FROM users WHERE email = 'camila.rocha@cardcorp.test'), 'CAMILA ROCHA', 'Antigo',      'AMEX',       '3782', '3307', '2030-07-31',  7000.00, 'CANCELLED', '2026-06-08 09:45:00+00', NULL);

-- Consultas rapidas para validacao manual
SELECT id, full_name, email FROM users ORDER BY created_at;
SELECT u.email, COUNT(c.id) AS total_cards
FROM users u
LEFT JOIN credit_cards c ON c.user_id = u.id
GROUP BY u.email
ORDER BY u.email;
SELECT c.*
FROM credit_cards c
JOIN users u ON u.id = c.user_id
WHERE u.email = 'mariana.alves@cardcorp.test'
ORDER BY c.created_at DESC
LIMIT 10;
