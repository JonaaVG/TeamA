<!-- components/base/DataList.vue -->
<template>
    <div class="space-y-4" role="region" :aria-label="ariaLabel">
        <!-- Emptystate handling -->
        <div v-if="!items || items.length === 0" tabindex="0"
            class="text-center py-12 bg-white rounded-xl border border-gray-200 text-gray-500 focus:outline-none focus:ring-4 focus:ring-blue-300">
            {{ emptyMessage }}
        </div>

        <!-- Generic list-container -->
        <ul v-else class="space-y-3 list-none p-0 m-0">
            <li v-for="item in items" :key="item[keyField]">
                <!-- SCOPED SLOT: Give the item data to the parent -->
                <slot name="item" :item="item" />
            </li>
        </ul>
    </div>
</template>

<script setup>
defineProps({
    items: {
        type: Array,
        required: true
    },
    keyField: {
        type: String,
        default: 'id'
    },
    emptyMessage: {
        type: String,
        default: 'Geen items gevonden.'
    },
    ariaLabel: {
        type: String,
        default: 'Lijst van items'
    }
})
</script>