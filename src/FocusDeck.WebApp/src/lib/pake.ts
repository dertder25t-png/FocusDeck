const SRP_MODULUS_HEX =
  'AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050' +
  'A37329CBB4A099ED8193E0757767A13DD52312AB4B03310DCD7F48A9DA04FD50' +
  'E8083969EDB767B0CF6096A4FA3B58F90F6A54B42A59D53B3A2A7C5F4F5F4E46' +
  '2E9F6A4E128E71B9F0C67C8E18CBF4C3BAFE8A31C5CFFFB4E90D54BD45BF37DF' +
  '365C1A65E68CFDA76D4DA708DF1FB2BC2E4A4371'

const SRP_MODULUS = BigInt('0x' + SRP_MODULUS_HEX)
const SRP_GENERATOR = 2n
const PAD_LENGTH = Math.ceil(SRP_MODULUS_HEX.length / 2)

interface SrpKdfParameters {
  alg?: string
  salt: string
  p?: number
  t?: number
  m?: number
}

interface LoginStartResponse {
  kdfParametersJson?: string | null
  saltBase64: string
  serverPublicEphemeralBase64: string
  sessionId: string
  algorithm: string
  modulusHex: string
  generator: number
}

interface LoginFinishResponse {
  success: boolean
  hasVault: boolean
  accessToken: string
  refreshToken: string
  expiresIn: number
  serverProofBase64: string
}

interface RegisterStartResponse {
  kdfParametersJson: string
  algorithm: string
  modulusHex: string
  generator: number
}

function ensureWebCrypto() {
  if (typeof crypto === 'undefined' || !crypto.subtle) {
    throw new Error('WebCrypto is required for PAKE flows')
  }
}

function bytesToBase64(bytes: Uint8Array): string {
  let binary = ''
  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i])
  }
  return btoa(binary)
}

function base64ToBytes(b64: string): Uint8Array {
  const bin = atob(b64)
  const bytes = new Uint8Array(bin.length)
  for (let i = 0; i < bin.length; i++) {
    bytes[i] = bin.charCodeAt(i)
  }
  return bytes
}

function bytesToBigInt(bytes: Uint8Array): bigint {
  let hex = ''
  for (const b of bytes) {
    hex += b.toString(16).padStart(2, '0')
  }
  if (hex.length === 0) {
    return 0n
  }
  return BigInt('0x' + hex)
}

function bigIntToBytes(value: bigint): Uint8Array {
  if (value === 0n) {
    return new Uint8Array([0])
  }
  let hex = value.toString(16)
  if (hex.length % 2 !== 0) {
    hex = '0' + hex
  }
  const bytes = new Uint8Array(hex.length / 2)
  for (let i = 0; i < bytes.length; i++) {
    bytes[i] = parseInt(hex.substr(i * 2, 2), 16)
  }
  return bytes
}

function pad(value: bigint): Uint8Array {
  const bytes = bigIntToBytes(value)
  if (bytes.length === PAD_LENGTH) {
    return bytes
  }
  if (bytes.length > PAD_LENGTH) {
    return bytes.slice(bytes.length - PAD_LENGTH)
  }
  const padded = new Uint8Array(PAD_LENGTH)
  padded.set(bytes, PAD_LENGTH - bytes.length)
  return padded
}

async function sha256(data: Uint8Array): Promise<Uint8Array> {
  ensureWebCrypto()
  const digest = await crypto.subtle.digest('SHA-256', data)
  return new Uint8Array(digest)
}

async function hashMultiple(...parts: Uint8Array[]): Promise<Uint8Array> {
  const totalLength = parts.reduce((sum, part) => sum + part.length, 0)
  const buffer = new Uint8Array(totalLength)
  let offset = 0
  for (const part of parts) {
    buffer.set(part, offset)
    offset += part.length
  }
  return sha256(buffer)
}

async function hashToBigInt(...parts: Uint8Array[]): Promise<bigint> {
  const digest = await hashMultiple(...parts)
  return bytesToBigInt(digest)
}

function mod(value: bigint, modulus: bigint): bigint {
  const result = value % modulus
  return result >= 0n ? result : result + modulus
}

function modPow(base: bigint, exponent: bigint, modulus: bigint): bigint {
  if (modulus === 1n) {
    return 0n
  }
  let result = 1n
  let b = mod(base, modulus)
  let e = exponent
  while (e > 0n) {
    if ((e & 1n) === 1n) {
      result = mod(result * b, modulus)
    }
    e >>= 1n
    b = mod(b * b, modulus)
  }
  return result
}

let multiplierPromise: Promise<bigint> | null = null

async function computeMultiplier(): Promise<bigint> {
  if (!multiplierPromise) {
    multiplierPromise = hashToBigInt(pad(SRP_MODULUS), pad(SRP_GENERATOR))
  }
  return multiplierPromise
}

function randomBigInt(): bigint {
  ensureWebCrypto()
  while (true) {
    const bytes = new Uint8Array(PAD_LENGTH)
    crypto.getRandomValues(bytes)
    const value = bytesToBigInt(bytes)
    if (value > 0n && value < SRP_MODULUS) {
      return value
    }
  }
}

async function computeLegacyPrivateKey(saltB64: string, userId: string, password: string): Promise<bigint> {
  const encoder = new TextEncoder()
  const salt = base64ToBytes(saltB64)
  const userPass = encoder.encode(`${userId}:${password}`)
  const inner = await sha256(userPass)
  const combined = new Uint8Array(salt.length + inner.length)
  combined.set(salt, 0)
  combined.set(inner, salt.length)
  const digest = await sha256(combined)
  return bytesToBigInt(digest)
}

function parseKdf(json?: string | null): SrpKdfParameters | null {
  if (!json) {
    return null
  }
  try {
    return JSON.parse(json) as SrpKdfParameters
  } catch {
    return null
  }
}

async function derivePrivateKey(kdf: SrpKdfParameters | null, saltB64Fallback: string, userId: string, password: string): Promise<bigint> {
  const salt = kdf?.salt ?? saltB64Fallback
  if (!salt) {
    throw new Error('Missing KDF salt')
  }
  return computeLegacyPrivateKey(salt, userId, password)
}

function arraysEqual(a: Uint8Array, b: Uint8Array): boolean {
  if (a.length !== b.length) {
    return false
  }
  let diff = 0
  for (let i = 0; i < a.length; i++) {
    diff |= a[i] ^ b[i]
  }
  return diff === 0
}

async function computeSessionKey(sessionSecret: bigint): Promise<Uint8Array> {
  return hashMultiple(pad(sessionSecret))
}

export async function pakeRegister(userId: string, password: string) {
  const startResponse = await fetch('/v1/auth/pake/register/start', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId }),
  })

  if (!startResponse.ok) {
    throw new Error('PAKE register start failed')
  }

  const start: RegisterStartResponse = await startResponse.json()

  if (start.algorithm !== 'SRP-6a-2048-SHA256' || start.modulusHex !== SRP_MODULUS_HEX || start.generator !== 2) {
    throw new Error('Unsupported SRP parameters from server')
  }

  const kdf = parseKdf(start.kdfParametersJson)
  const saltB64 = kdf?.salt

  if (!saltB64) {
    throw new Error('Server did not provide registration salt')
  }

  const privateKey = await derivePrivateKey(kdf, saltB64, userId, password)
  const verifier = modPow(SRP_GENERATOR, privateKey, SRP_MODULUS)
  const verifierB64 = bytesToBase64(bigIntToBytes(verifier))

  const finishResponse = await fetch('/v1/auth/pake/register/finish', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId,
      verifierBase64: verifierB64,
      kdfParametersJson: start.kdfParametersJson,
      vaultDataBase64: null,
      vaultKdfMetadataJson: null,
      vaultCipherSuite: null,
    }),
  })

  if (!finishResponse.ok) {
    const error = await finishResponse.json().catch(() => ({}))
    throw new Error(error?.error || 'PAKE register finish failed')
  }

  return finishResponse.json()
}

export async function pakeLogin(userId: string, password: string, options?: { clientId?: string; deviceName?: string; devicePlatform?: string }) {
  const clientSecret = randomBigInt()
  const clientPublic = modPow(SRP_GENERATOR, clientSecret, SRP_MODULUS)
  const clientPublicB64 = bytesToBase64(bigIntToBytes(clientPublic))

  const startRes = await fetch('/v1/auth/pake/login/start', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId,
      clientPublicEphemeralBase64: clientPublicB64,
      clientId: options?.clientId ?? navigator.userAgent,
      deviceName: options?.deviceName ?? navigator.userAgent,
      devicePlatform: options?.devicePlatform ?? 'web',
    }),
  })

  if (!startRes.ok) {
    const error = await startRes.json().catch(() => ({}))
    throw new Error(error?.error || 'PAKE login start failed')
  }

  const start: LoginStartResponse = await startRes.json()

  if (start.algorithm !== 'SRP-6a-2048-SHA256' || start.modulusHex !== SRP_MODULUS_HEX || start.generator !== 2) {
    throw new Error('Unsupported SRP parameters from server')
  }

  const serverPublic = bytesToBigInt(base64ToBytes(start.serverPublicEphemeralBase64))
  if (serverPublic <= 0n || serverPublic % SRP_MODULUS === 0n) {
    throw new Error('Invalid server public ephemeral value')
  }

  const kdf = parseKdf(start.kdfParametersJson)
  const privateKey = await derivePrivateKey(kdf, start.saltBase64, userId, password)
  const scramble = await hashToBigInt(pad(clientPublic), pad(serverPublic))
  if (scramble === 0n) {
    throw new Error('SRP scramble parameter was zero')
  }

  const k = await computeMultiplier()
  const gx = modPow(SRP_GENERATOR, privateKey, SRP_MODULUS)
  const tmp = mod(serverPublic - k * gx, SRP_MODULUS)
  const exponent = mod(clientSecret + scramble * privateKey, SRP_MODULUS)
  const sessionSecret = modPow(tmp, exponent, SRP_MODULUS)
  const sessionKey = await computeSessionKey(sessionSecret)
  const clientProofBytes = await hashMultiple(pad(clientPublic), pad(serverPublic), sessionKey)
  const clientProofBase64 = bytesToBase64(clientProofBytes)

  const finishRes = await fetch('/v1/auth/pake/login/finish', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId,
      sessionId: start.sessionId,
      clientProofBase64,
      clientId: options?.clientId ?? navigator.userAgent,
      deviceName: options?.deviceName ?? navigator.userAgent,
      devicePlatform: options?.devicePlatform ?? 'web',
    }),
  })

  if (!finishRes.ok) {
    const error = await finishRes.json().catch(() => ({}))
    throw new Error(error?.error || 'PAKE login finish failed')
  }

  const finish: LoginFinishResponse = await finishRes.json()
  if (!finish.success) {
    throw new Error('Login failed')
  }

  const expectedServerProof = await hashMultiple(pad(clientPublic), clientProofBytes, sessionKey)
  const actualServerProof = base64ToBytes(finish.serverProofBase64)
  if (!arraysEqual(expectedServerProof, actualServerProof)) {
    throw new Error('Server proof validation failed')
  }

  return finish
}

export async function startPairing() {
  const res = await fetch('/v1/auth/pake/pair/start', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sourceDeviceId: navigator.userAgent }),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error?.error || 'Failed to start pairing')
  }
  return res.json()
}

export async function redeemPairing(pairingId: string, code: string) {
  const res = await fetch('/v1/auth/pake/pair/redeem', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ pairingId, code }),
  })
  if (!res.ok) {
    const error = await res.json().catch(() => ({}))
    throw new Error(error?.error || 'Failed to redeem pairing code')
  }
  return res.json()
}


