<template>
  <p
    class="message"
    :class="[variant, { 'message-empty': empty }]"
    :role="variant === 'error' ? 'alert' : 'status'"
    :aria-live="variant === 'error' ? 'assertive' : 'polite'"
    aria-atomic="true"
  >
    <slot />
  </p>
</template>

<script setup>
defineProps({
  // Selects the visual style and the appropriate screen-reader announcement priority.
  variant: {
    type: String,
    default: 'success',
    validator: (value) => ['success', 'error'].includes(value),
  },
  // Empty live regions remain in the page so future messages can be announced accessibly.
  empty: {
    type: Boolean,
    default: false,
  },
})
</script>

<style scoped>
.message {
  margin: 12px 0;
  padding: 10px;
  border-radius: 6px;
}

/* Keep the live region mounted when empty so assistive technology can announce later updates. */
.message-empty {
  margin: 0;
  padding: 0;
}

.success {
  color: #166534;
  background: #dcfce7;
}

.error {
  color: #991b1b;
  background: #fee2e2;
}
</style>
