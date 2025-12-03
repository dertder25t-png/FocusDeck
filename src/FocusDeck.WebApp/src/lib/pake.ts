import argon2WasmUrl from 'argon2-browser/dist/argon2.wasm?url'
import { getAuthToken } from './utils'

type Argon2Globals = typeof globalThis & {
  argon2WasmPath?: string
  loadArgon2WasmBinary?: () => Promise<ArrayBuffer> | ArrayBuffer
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  loadArgon2WasmModule?: () => Promise<any>
}

const globalScope = globalThis as Argon2Globals
globalScope.argon2WasmPath = globalScope.argon2WasmPath ?? argon2WasmUrl

let wasmArrayBufferPromise: Promise<ArrayBuffer> | null = null

function fetchArgon2Wasm(): Promise<ArrayBuffer> {
  return fetch(argon2WasmUrl).then((res) => {
    if (!res.ok) {
      throw new Error('Failed to load Argon2 WASM')
    }
    return res.arrayBuffer()
  })
}

globalScope.loadArgon2WasmBinary = () => {
  if (!wasmArrayBufferPromise) {
    wasmArrayBufferPromise = fetchArgon2Wasm()
  }
  return wasmArrayBufferPromise
}
globalScope.loadArgon2WasmModule = undefined

let argon2ModulePromise: Promise<typeof import('argon2-browser')> | null = null

async function ensureArgon2() {
  if (!argon2ModulePromise) {
    argon2ModulePromise = import('argon2-browser')
  }
  return argon2ModulePromise
}

// Default constants, but we should prefer values from the server
const DEFAULT_SRP_MODULUS_HEX =
  'AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050' +
  'A37329CBB4A099ED8193E0757767A13DD52312AB4B03310DCD7F48A9DA04FD50' +
  'E8083969EDB767B0CF6096A4FA3B58F90F6A54B42A59D53B3A2A7C5F4F5F4E46' +
  '2E9F6A4E128E71B9F0C67C8E18CBF4C3BAFE8A31C5CFFFB4E90D54BD45BF37DF' +
  '365C1A65E68CFDA76D4DA708DF1FB2BC2E4A4371'

interface SrpKdfParameters {
  alg?: string
  salt: string
  p?: number
  t?: number
  m?: number
  aad?: boolean
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
  if (typeof crypto === 'undefined') {
    throw new Error('WebCrypto is required for PAKE flows')
  }
  // Note: crypto.subtle may not be available in non-HTTPS contexts
  // We'll handle that with a fallback SHA-256 implementation
}

// Fallback SHA-256 implementation for non-secure contexts (HTTP)
// This is a pure JavaScript implementation based on the SHA-256 specification
function sha256Fallback(data: Uint8Array): Uint8Array {
  // SHA-256 constants
  const K = new Uint32Array([
    0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
    0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
    0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
    0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
    0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
    0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
    0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
    0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
  ])

  const rotr = (x: number, n: number) => (x >>> n) | (x << (32 - n))
  const ch = (x: number, y: number, z: number) => (x & y) ^ (~x & z)
  const maj = (x: number, y: number, z: number) => (x & y) ^ (x & z) ^ (y & z)
  const sigma0 = (x: number) => rotr(x, 2) ^ rotr(x, 13) ^ rotr(x, 22)
  const sigma1 = (x: number) => rotr(x, 6) ^ rotr(x, 11) ^ rotr(x, 25)
  const gamma0 = (x: number) => rotr(x, 7) ^ rotr(x, 18) ^ (x >>> 3)
  const gamma1 = (x: number) => rotr(x, 17) ^ rotr(x, 19) ^ (x >>> 10)

  // Pre-processing
  const msgLen = data.length
  const bitLen = msgLen * 8
  
  // Padding
  const paddedLen = Math.ceil((msgLen + 9) / 64) * 64
  const padded = new Uint8Array(paddedLen)
  padded.set(data)
  padded[msgLen] = 0x80
  
  // Append length as 64-bit big-endian
  const view = new DataView(padded.buffer)
  view.setUint32(paddedLen - 4, bitLen >>> 0, false)
  view.setUint32(paddedLen - 8, (bitLen / 0x100000000) >>> 0, false)

  // Initialize hash values
  let h0 = 0x6a09e667, h1 = 0xbb67ae85, h2 = 0x3c6ef372, h3 = 0xa54ff53a
  let h4 = 0x510e527f, h5 = 0x9b05688c, h6 = 0x1f83d9ab, h7 = 0x5be0cd19

  // Process each 512-bit chunk
  const w = new Uint32Array(64)
  for (let chunk = 0; chunk < paddedLen; chunk += 64) {
    // Break chunk into sixteen 32-bit big-endian words
    for (let i = 0; i < 16; i++) {
      w[i] = view.getUint32(chunk + i * 4, false)
    }
    
    // Extend the sixteen 32-bit words into sixty-four 32-bit words
    for (let i = 16; i < 64; i++) {
      w[i] = (gamma1(w[i - 2]) + w[i - 7] + gamma0(w[i - 15]) + w[i - 16]) >>> 0
    }

    // Initialize working variables
    let a = h0, b = h1, c = h2, d = h3, e = h4, f = h5, g = h6, h = h7

    // Main loop
    for (let i = 0; i < 64; i++) {
      const t1 = (h + sigma1(e) + ch(e, f, g) + K[i] + w[i]) >>> 0
      const t2 = (sigma0(a) + maj(a, b, c)) >>> 0
      h = g
      g = f
      f = e
      e = (d + t1) >>> 0
      d = c
      c = b
      b = a
      a = (t1 + t2) >>> 0
    }

    // Add this chunk's hash to result
    h0 = (h0 + a) >>> 0
    h1 = (h1 + b) >>> 0
    h2 = (h2 + c) >>> 0
    h3 = (h3 + d) >>> 0
    h4 = (h4 + e) >>> 0
    h5 = (h5 + f) >>> 0
    h6 = (h6 + g) >>> 0
    h7 = (h7 + h) >>> 0
  }

  // Produce the final hash value (big-endian)
  const hash = new Uint8Array(32)
  const hashView = new DataView(hash.buffer)
  hashView.setUint32(0, h0, false)
  hashView.setUint32(4, h1, false)
  hashView.setUint32(8, h2, false)
  hashView.setUint32(12, h3, false)
  hashView.setUint32(16, h4, false)
  hashView.setUint32(20, h5, false)
  hashView.setUint32(24, h6, false)
  hashView.setUint32(28, h7, false)

  return hash
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

function pad(value: bigint, length: number): Uint8Array {
  const bytes = bigIntToBytes(value)
  if (bytes.length === length) {
    return bytes
  }
  if (bytes.length > length) {
    return bytes.slice(bytes.length - length)
  }
  const padded = new Uint8Array(length)
  padded.set(bytes, length - bytes.length)
  return padded
}

async function sha256(data: Uint8Array): Promise<Uint8Array> {
  ensureWebCrypto()
  
  // Use native crypto.subtle if available (HTTPS or localhost)
  if (crypto.subtle) {
    const bufferSlice = data.buffer.slice(data.byteOffset, data.byteOffset + data.byteLength) as ArrayBuffer
    const digest = await crypto.subtle.digest('SHA-256', bufferSlice)
    return new Uint8Array(digest)
  }
  
  // Fallback to JavaScript implementation for HTTP contexts
  return sha256Fallback(data)
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

// Cache multipliers by modulus/generator key
const multiplierCache = new Map<string, Promise<bigint>>()

async function computeMultiplier(modulus: bigint, generator: bigint): Promise<bigint> {
  const key = modulus.toString(16) + ':' + generator.toString()
  if (!multiplierCache.has(key)) {
    // Pad length depends on modulus size
    const padLen = Math.ceil(modulus.toString(16).length / 2)
    multiplierCache.set(key, hashToBigInt(pad(modulus, padLen), pad(generator, padLen)))
  }
  return multiplierCache.get(key)!
}

function randomBigInt(modulus: bigint): bigint {
  ensureWebCrypto()
  const padLen = Math.ceil(modulus.toString(16).length / 2)
  while (true) {
    const bytes = new Uint8Array(padLen)
    crypto.getRandomValues(bytes)
    const value = bytesToBigInt(bytes)
    if (value > 0n && value < modulus) {
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

async function computeArgon2idPrivateKey(kdf: SrpKdfParameters, userId: string, password: string): Promise<bigint> {
  if (!kdf?.salt) {
    throw new Error('Missing Argon2id salt')
  }

  const encoder = new TextEncoder()
  const passwordBytes = encoder.encode(password)
  const associatedData = kdf.aad === false ? null : encoder.encode(userId)
  const saltBytes = base64ToBytes(kdf.salt)

  const time = kdf.t ?? 3
  const mem = kdf.m ?? 65536
  const parallelism = kdf.p ?? 2

  const { ArgonType, hash: argon2Hash } = await ensureArgon2()

  const result = await argon2Hash({
    pass: passwordBytes,
    salt: saltBytes,
    time,
    mem,
    parallelism,
    hashLen: 32,
    type: ArgonType.id,
    ...(associatedData ? { associatedData } : {}),
  })

  return bytesToBigInt(new Uint8Array(result.hash))
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
  // Use Argon2id if the credential was provisioned with it
  if (kdf?.alg?.toLowerCase() === 'argon2id') {
    return await computeArgon2idPrivateKey(kdf, userId, password)
  }

  // Fall back to the legacy SHA256 derivation that older credentials expect
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

async function computeSessionKey(sessionSecret: bigint, padLength: number): Promise<Uint8Array> {
  return hashMultiple(pad(sessionSecret, padLength))
}

export async function pakeRegister(userId: string, password: string) {
  const startResponse = await fetch('/v1/auth/pake/register/start', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, devicePlatform: 'web' }),
  })

  if (!startResponse.ok) {
    throw new Error('PAKE register start failed')
  }

  const start: RegisterStartResponse = await startResponse.json()

  // Use server parameters
  const modulus = BigInt('0x' + start.modulusHex)
  const generator = BigInt(start.generator)

  const kdf = parseKdf(start.kdfParametersJson)
  const saltB64 = kdf?.salt

  if (!saltB64) {
    throw new Error('Server did not provide registration salt')
  }

  const privateKey = await derivePrivateKey(kdf, saltB64, userId, password)
  const verifier = modPow(generator, privateKey, modulus)
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
  // Use defaults for the initial ephemeral key generation.
  // This allows the initial handshake to proceed.
  const initialModulus = BigInt('0x' + DEFAULT_SRP_MODULUS_HEX)
  const initialGenerator = 2n

  const clientSecret = randomBigInt(initialModulus)
  const clientPublic = modPow(initialGenerator, clientSecret, initialModulus)
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

  // Use server provided parameters for the rest of calculation
  const modulus = BigInt('0x' + start.modulusHex)
  const generator = BigInt(start.generator)
  const padLen = Math.ceil(start.modulusHex.length / 2)

  const serverPublic = bytesToBigInt(base64ToBytes(start.serverPublicEphemeralBase64))
  if (serverPublic <= 0n || serverPublic % modulus === 0n) {
    throw new Error('Invalid server public ephemeral value')
  }

  const kdf = parseKdf(start.kdfParametersJson)
  const privateKey = await derivePrivateKey(kdf, start.saltBase64, userId, password)

  const scramble = await hashToBigInt(pad(clientPublic, padLen), pad(serverPublic, padLen))
  if (scramble === 0n) {
    throw new Error('SRP scramble parameter was zero')
  }

  const k = await computeMultiplier(modulus, generator)
  const gx = modPow(generator, privateKey, modulus)
  const tmp = mod(serverPublic - k * gx, modulus)
  const exponent = mod(clientSecret + scramble * privateKey, modulus)
  const sessionSecret = modPow(tmp, exponent, modulus)
  const sessionKey = await computeSessionKey(sessionSecret, padLen)
  const clientProofBytes = await hashMultiple(pad(clientPublic, padLen), pad(serverPublic, padLen), sessionKey)
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

  const expectedServerProof = await hashMultiple(pad(clientPublic, padLen), clientProofBytes, sessionKey)
  const actualServerProof = base64ToBytes(finish.serverProofBase64)
  if (!arraysEqual(expectedServerProof, actualServerProof)) {
    throw new Error('Server proof validation failed')
  }

  return finish
}

export async function startPairing() {
  const token = await getAuthToken()
  const res = await fetch('/v1/auth/pake/pair/start', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
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
