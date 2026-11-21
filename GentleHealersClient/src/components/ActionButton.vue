<template>
  <button
    :type="type"
    class="button"
    :class="`button-${variant}`"
    :disabled="disabled"
  >
    <slot />
  </button>
</template>

<script setup>
defineProps({
  // Selects a shared button color while leaving the button text and action to the parent.
  variant: {
    type: String,
    default: 'primary',
    validator: (value) => ['primary', 'danger'].includes(value),
  },
  // Pass through the native button type to support forms and ordinary actions.
  type: {
    type: String,
    default: 'button',
  },
  // Native disabled state, useful while an action is being processed.
  disabled: {
    type: Boolean,
    default: false,
  },
})
</script>

<style scoped>
.button {
  min-height: 31px;
  padding: 0 8px;
  border: 0;
  border-radius: 8px;
  color: white;
  font-size: 10px;
  font-weight: 700;
  white-space: nowrap;
  cursor: pointer;
}

.button-primary {
  background: #153b70;
}

.button-danger {
  background: #810b0b;
}

.button:disabled {
  cursor: wait;
  opacity: 0.6;
}

@media (max-width: 600px) {
  .button {
    min-height: 40px;
    font-size: 14px;
  }
}
</style>
