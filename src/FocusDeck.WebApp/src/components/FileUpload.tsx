import { useState, useRef, type ChangeEvent } from 'react'
import { Button } from './Button'
import { cn } from '../lib/utils'

interface FileUploadProps {
  accept?: string
  maxSize?: number // in bytes
  onUpload: (file: File) => Promise<void>
  className?: string
}

export function FileUpload({ accept, maxSize = 5 * 1024 * 1024, onUpload, className }: FileUploadProps) {
  const [isDragging, setIsDragging] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [progress, setProgress] = useState(0)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const validateFile = (file: File): string | null => {
    // Check file size
    if (file.size > maxSize) {
      return `File size exceeds ${Math.round(maxSize / 1024 / 1024)}MB limit`
    }

    // Check file type if accept is specified
    if (accept) {
      const acceptedTypes = accept.split(',').map(t => t.trim())
      const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase()
      const mimeType = file.type

      const isAccepted = acceptedTypes.some(type => {
        if (type.startsWith('.')) {
          return fileExtension === type.toLowerCase()
        }
        if (type.includes('*')) {
          const baseType = type.split('/')[0]
          return mimeType.startsWith(baseType)
        }
        return mimeType === type
      })

      if (!isAccepted) {
        return `File type not accepted. Allowed: ${accept}`
      }
    }

    return null
  }

  const handleFileSelect = (selectedFile: File) => {
    setError(null)
    const validationError = validateFile(selectedFile)
    
    if (validationError) {
      setError(validationError)
      return
    }

    setFile(selectedFile)
  }

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      handleFileSelect(selectedFile)
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(true)
  }

  const handleDragLeave = () => {
    setIsDragging(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)

    const droppedFile = e.dataTransfer.files[0]
    if (droppedFile) {
      handleFileSelect(droppedFile)
    }
  }

  const handleUpload = async () => {
    if (!file) return

    setUploading(true)
    setProgress(0)
    setError(null)

    try {
      // Simulate progress (in real implementation, use XMLHttpRequest or fetch with ReadableStream)
      const interval = setInterval(() => {
        setProgress(prev => {
          if (prev >= 90) {
            clearInterval(interval)
            return 90
          }
          return prev + 10
        })
      }, 200)

      await onUpload(file)
      
      clearInterval(interval)
      setProgress(100)
      
      // Reset after successful upload
      setTimeout(() => {
        setFile(null)
        setProgress(0)
        setUploading(false)
      }, 1000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed')
      setUploading(false)
      setProgress(0)
    }
  }

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  return (
    <div className={cn('space-y-4', className)}>
      {/* Drop Zone */}
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={cn(
          'border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors',
          isDragging ? 'border-primary bg-primary/5' : 'border-gray-700 hover:border-gray-600',
          error && 'border-red-500/50 bg-red-500/5'
        )}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept={accept}
          onChange={handleFileChange}
          className="hidden"
        />
        
        <div className="text-4xl mb-3">📁</div>
        
        {file ? (
          <div>
            <div className="font-medium text-lg mb-1">{file.name}</div>
            <div className="text-sm text-gray-400">{formatFileSize(file.size)}</div>
          </div>
        ) : (
          <div>
            <div className="font-medium mb-2">Drop file here or click to browse</div>
            <div className="text-sm text-gray-400">
              {accept ? `Accepted: ${accept}` : 'All file types accepted'}
              {' • '}
              Max size: {Math.round(maxSize / 1024 / 1024)}MB
            </div>
          </div>
        )}
      </div>

      {/* Error Message */}
      {error && (
        <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-md text-sm text-red-400">
          {error}
        </div>
      )}

      {/* Progress Bar */}
      {uploading && (
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-400">Uploading...</span>
            <span className="font-medium">{progress}%</span>
          </div>
          <div className="h-2 bg-gray-800 rounded-full overflow-hidden">
            <div
              className="h-full bg-primary transition-all duration-300"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      )}

      {/* Upload Button */}
      {file && !uploading && (
        <div className="flex gap-3">
          <Button onClick={handleUpload} disabled={uploading}>
            Upload
          </Button>
          <Button
            variant="secondary"
            onClick={() => {
              setFile(null)
              setError(null)
            }}
          >
            Cancel
          </Button>
        </div>
      )}
    </div>
  )
}
