import { reactive } from 'vue'

// A simple toast notification system.It allows you to show and dismiss toast messages with different types (e.g., success, warning, error)
// and automatically dismiss them after a specified duration.
const toasts = reactive([])
let nextId = 0

function dismiss(id) {
  const index = toasts.findIndex((toast) => toast.id === id)
  if (index !== -1) toasts.splice(index, 1)
}

function showToast(message, type = 'success', durationMs = 8000) {
  const id = nextId++
  toasts.push({ id, message, type })
  setTimeout(() => dismiss(id), durationMs)
}

export function useToast() {
  return { toasts, showToast, dismiss }
}