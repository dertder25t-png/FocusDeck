import type { ComponentType, CSSProperties, SyntheticEvent } from 'react'

declare module 'react-qr-reader' {
  interface QrReaderResult {
    text: string | null
  }

  interface QrReaderProps {
    onResult?: (result: QrReaderResult | null, error: Error | null) => void
    onError?: (error: Error | null) => void
    onLoad?: () => void
    onImageLoad?: (event: SyntheticEvent<HTMLImageElement>) => void
    delay?: number | false
    facingMode?: 'user' | 'environment'
    legacyMode?: boolean
    resolution?: number
    showViewFinder?: boolean
    style?: CSSProperties
    className?: string
    containerStyle?: CSSProperties
    constraints?: MediaTrackConstraints
  }

  const QrReader: ComponentType<QrReaderProps>

  export default QrReader
}
