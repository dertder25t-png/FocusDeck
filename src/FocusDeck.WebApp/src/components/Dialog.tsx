import * as DialogPrimitive from '@radix-ui/react-dialog'
import { cn } from '../lib/utils'

export function Dialog({ children, open, onOpenChange }: {
  children: React.ReactNode
  open?: boolean
  onOpenChange?: (open: boolean) => void
}) {
  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      {children}
    </DialogPrimitive.Root>
  )
}

export function DialogTrigger({ children, asChild }: {
  children: React.ReactNode
  asChild?: boolean
}) {
  return (
    <DialogPrimitive.Trigger asChild={asChild}>
      {children}
    </DialogPrimitive.Trigger>
  )
}

export function DialogContent({ children, className }: {
  children: React.ReactNode
  className?: string
}) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="fixed inset-0 bg-black/80 backdrop-blur-sm data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
      <DialogPrimitive.Content
        className={cn(
          'fixed left-[50%] top-[50%] z-50 w-full max-w-lg translate-x-[-50%] translate-y-[-50%] bg-[#1a1a1c] border border-gray-800 rounded-xl p-6 shadow-2xl',
          'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[state=closed]:slide-out-to-left-1/2 data-[state=closed]:slide-out-to-top-[48%] data-[state=open]:slide-in-from-left-1/2 data-[state=open]:slide-in-from-top-[48%]',
          'focus:outline-none',
          className
        )}
      >
        {children}
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  )
}

export function DialogHeader({ children, className }: {
  children: React.ReactNode
  className?: string
}) {
  return (
    <div className={cn('flex flex-col space-y-1.5 mb-4', className)}>
      {children}
    </div>
  )
}

export function DialogTitle({ children, className }: {
  children: React.ReactNode
  className?: string
}) {
  return (
    <DialogPrimitive.Title className={cn('text-lg font-semibold', className)}>
      {children}
    </DialogPrimitive.Title>
  )
}

export function DialogDescription({ children, className }: {
  children: React.ReactNode
  className?: string
}) {
  return (
    <DialogPrimitive.Description className={cn('text-sm text-gray-400', className)}>
      {children}
    </DialogPrimitive.Description>
  )
}

export function DialogFooter({ children, className }: {
  children: React.ReactNode
  className?: string
}) {
  return (
    <div className={cn('flex items-center justify-end gap-2 mt-6', className)}>
      {children}
    </div>
  )
}

export function DialogClose({ children, asChild }: {
  children: React.ReactNode
  asChild?: boolean
}) {
  return (
    <DialogPrimitive.Close asChild={asChild}>
      {children}
    </DialogPrimitive.Close>
  )
}
