import { useEffect, useRef } from 'react'
// @ts-ignore
import * as QRCode from 'qrcode'

type QrCodeProps = {
  value: string
  size?: number
  className?: string
  fgColor?: string
  bgColor?: string
  margin?: number
}

export function QrCode({
  value,
  size = 200,
  className,
  fgColor = '#000000',
  bgColor = '#ffffff',
  margin = 1,
}: QrCodeProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas || !value) return
    QRCode.toCanvas(canvas, value, {
      width: size,
      margin,
      color: { dark: fgColor, light: bgColor },
      errorCorrectionLevel: 'M',
    }).catch(() => {
      // noop – leave blank on error
    })
  }, [value, size, fgColor, bgColor, margin])

  return <canvas ref={canvasRef} width={size} height={size} className={className} />
}

export default QrCode

