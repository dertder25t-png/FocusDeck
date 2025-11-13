declare module '*.wasm?url' {
  const url: string
  export default url
}

export {}

declare global {
  var argon2WasmPath: string | undefined
  var loadArgon2WasmBinary: (() => Promise<ArrayBuffer> | ArrayBuffer) | undefined
  var loadArgon2WasmModule: (() => Promise<any>) | undefined

  interface Window {
    argon2WasmPath?: string
    loadArgon2WasmBinary?: (() => Promise<string | ArrayBuffer> | string | ArrayBuffer)
    loadArgon2WasmModule?: (() => Promise<any>)
  }
}
