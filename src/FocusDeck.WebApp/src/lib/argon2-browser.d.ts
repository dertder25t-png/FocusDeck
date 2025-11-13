declare module 'argon2-browser' {
  export enum ArgonType {
    i = 1,
    d = 2,
    id = 3,
  }

  export interface Argon2BrowserOptions {
    pass: Uint8Array
    salt: Uint8Array
    time: number
    mem: number
    parallelism: number
    hashLen: number
    type: ArgonType
    associatedData?: Uint8Array
    ad?: Uint8Array
  }

  export interface Argon2BrowserResult {
    hash: ArrayBuffer
    hashHex: string
    encoded: string
  }

  export function hash(options: Argon2BrowserOptions): Promise<Argon2BrowserResult>
}
