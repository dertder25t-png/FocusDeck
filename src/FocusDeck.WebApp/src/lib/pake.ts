export async function pbkdf2(password: string, saltB64: string, iterations = 100000): Promise<Uint8Array> {
  const enc = new TextEncoder()
  const baseKey = await crypto.subtle.importKey('raw', enc.encode(password), 'PBKDF2', false, ['deriveBits'])
  const salt = base64ToBytes(saltB64)
  const bits = await crypto.subtle.deriveBits({ name: 'PBKDF2', salt, iterations, hash: 'SHA-256' }, baseKey, 256)
  return new Uint8Array(bits)
}

export async function hmacSha256(keyBytes: Uint8Array, data: string): Promise<string> {
  const key = await crypto.subtle.importKey('raw', keyBytes, { name: 'HMAC', hash: 'SHA-256' }, false, ['sign'])
  const sig = await crypto.subtle.sign('HMAC', key, new TextEncoder().encode(data))
  return bytesToBase64(new Uint8Array(sig))
}

export function bytesToBase64(bytes: Uint8Array): string {
  let binary = ''
  for (let i = 0; i < bytes.byteLength; i++) binary += String.fromCharCode(bytes[i])
  return btoa(binary)
}

export function base64ToBytes(b64: string): Uint8Array {
  const bin = atob(b64)
  const bytes = new Uint8Array(bin.length)
  for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i)
  return bytes
}

export async function pakeLogin(userId: string, password: string) {
  const startRes = await fetch('/v1/auth/pake/login/start', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ userId })
  })
  if (!startRes.ok) throw new Error('PAKE start failed')
  const { salt, challenge } = await startRes.json()
  const key = await pbkdf2(password, salt)
  const clientProof = await hmacSha256(key, challenge)
  const finishRes = await fetch('/v1/auth/pake/login/finish', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, challenge, clientProof, clientId: navigator.userAgent })
  })
  if (!finishRes.ok) throw new Error('PAKE finish failed')
  return await finishRes.json()
}

