<script setup>
import { CircleCheck, CircleAlert, TriangleAlert, Info, X } from 'lucide-vue-next'
import nl from './nl.json'
import { useToast } from './useToast'

const t = nl.toast

const { toasts, dismiss } = useToast()

const variants = {
  success: {
    box: 'border-green-600 bg-green-50 text-green-950',
    icon: CircleCheck,
    iconClass: 'text-green-700',
  },
  error: {
    box: 'border-red-600 bg-red-50 text-red-950',
    icon: CircleAlert,
    iconClass: 'text-red-700',
  },
  warning: {
    box: 'border-amber-500 bg-amber-50 text-amber-950',
    icon: TriangleAlert,
    iconClass: 'text-amber-600',
  },
  info: {
    box: 'border-blue-600 bg-blue-50 text-blue-950',
    icon: Info,
    iconClass: 'text-blue-700',
  },
}

const variantOf = (type) => variants[type] ?? variants.info
</script>

<template>
  <div
    class="pointer-events-none fixed inset-x-0 top-20 z-50 flex justify-center px-4"
    role="status"
    aria-live="polite"
  >
    <TransitionGroup
      tag="div"
      class="flex w-full max-w-xl flex-col gap-3"
      enter-from-class="opacity-0 -translate-y-4"
      enter-active-class="transition duration-300 ease-out"
      leave-active-class="transition duration-200 ease-in"
      leave-to-class="opacity-0"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        :class="[
          'pointer-events-auto flex items-start gap-3 rounded-xl border-2 border-l-8 px-5 py-4 shadow-xl',
          variantOf(toast.type).box,
        ]"
      >
        <component
          :is="variantOf(toast.type).icon"
          :class="['mt-0.5 size-6 shrink-0', variantOf(toast.type).iconClass]"
          aria-hidden="true"
        />
        <span class="flex-1 text-base font-medium">{{ toast.message }}</span>
        <button
          type="button"
          class="rounded p-1 opacity-70 transition hover:bg-black/10 hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-current"
          :aria-label="t.close"
          @click="dismiss(toast.id)"
        >
          <X class="size-5" aria-hidden="true" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>