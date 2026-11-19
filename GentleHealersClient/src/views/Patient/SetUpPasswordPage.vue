<script setup>
import {useRoute} from 'vue-router'
import {ref, onMounted} from 'vue'

const route = useRoute();
const status = ref('Bezig met activeren...')

var token = route.query.token
var email = route.query.email
var isValid = false

onMounted(async() => {
    if (!token || !email) {
        status.value = 'ongeldige activatielink'
        return
    }
    try {
        const response = await fetch('http://localhost:7113/api/account/activate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                token, email
            })
        })
        if (!response.ok) {
            throw new Error ('Activatie mislukt')
        }

        status.value = 'je account is succesvol geactiveerd'
        isValid.value = true
    } catch (error) {
        status.value = 'de activatielink is ongeldig of verlopen'
    }
})

</script>
<template>
    <div class="min-h-screen bg-[#e8f3fc] flex items-center justify-center p-6">
    <div class="w-full max-w-[530px] bg-white rounded-2xl shadow-sm border border-slate-300 px-8 py-10">

        <!-- Logo -->
        <div class="text-center text-[1.75rem] font-bold tracking-tight" aria-label="Zachte Heelmeesters">
            <span class="text-[#12376b]">Zachte</span>
            <span class="text-[#0a76d8]">Heelmeesters</span>
        </div>
        <hr class="mb-6 mt-4 h-1 rounded-sm border-0 bg-[#e3eefb]" />

        <!-- Titel en subtitel -->
        <div class="mb-6">
        <h1 class="text-2xl font-bold text-[#123668]">Nieuw wachtwoord</h1>
        <p class="text-sm text-[#123668] mt-1">
            Kies een nieuw wachtwoord voor je account en bevestig het hieronder.
        </p>
        </div>

        <form class="space-y-5">

        <!-- Nieuw wachtwoord -->
        <div class="space-y-2">
            <label for="password" class="block text-sm font-semibold text-[#123668]">Nieuw wachtwoord</label>
            <div class="relative">
            <svg class="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-[#123668]" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 0 0-5.25 5.25v3a3 3 0 0 0-3 3v6.75a3 3 0 0 0 3 3h10.5a3 3 0 0 0 3-3v-6.75a3 3 0 0 0-3-3v-3A5.25 5.25 0 0 0 12 1.5Zm3.75 8.25v-3a3.75 3.75 0 1 0-7.5 0v3h7.5Z" clip-rule="evenodd" />
            </svg>

            <input
                id="password"
                type="password"
                placeholder="Wachtwoord"
                class="w-full pl-12 pr-12 py-3 rounded-xl border-2 border-[#123668] text-[#123668] placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#0a78d1]"
            />

            <button type="button" class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 hover:text-[#123668] transition">
                <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 3l18 18" />
                <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                <path d="M9.4 5.5A9.8 9.8 0 0 1 12 5c5 0 8.5 4 9.5 7a11.7 11.7 0 0 1-3 4.4M6.5 6.6A11.7 11.7 0 0 0 2.5 12c1 3 4.5 7 9.5 7 1.5 0 2.9-.4 4.1-1" />
                </svg>
            </button>
            </div>
        </div>

        <!-- Herhaal wachtwoord -->
        <div class="space-y-2">
            <label for="confirmPassword" class="block text-sm font-semibold text-[#123668]">Herhaal je wachtwoord</label>
            <div class="relative">
            <svg class="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-[#123668]" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 0 0-5.25 5.25v3a3 3 0 0 0-3 3v6.75a3 3 0 0 0 3 3h10.5a3 3 0 0 0 3-3v-6.75a3 3 0 0 0-3-3v-3A5.25 5.25 0 0 0 12 1.5Zm3.75 8.25v-3a3.75 3.75 0 1 0-7.5 0v3h7.5Z" clip-rule="evenodd" />
            </svg>

            <input
                id="confirmPassword"
                type="password"
                placeholder="Wachtwoord"
                class="w-full pl-12 pr-12 py-3 rounded-xl border-2 border-[#123668] text-[#123668] placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#0a78d1]"
            />

            <button type="button" class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 hover:text-[#123668] transition">
                <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 3l18 18" />
                <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                <path d="M9.4 5.5A9.8 9.8 0 0 1 12 5c5 0 8.5 4 9.5 7a11.7 11.7 0 0 1-3 4.4M6.5 6.6A11.7 11.7 0 0 0 2.5 12c1 3 4.5 7 9.5 7 1.5 0 2.9-.4 4.1-1" />
                </svg>
            </button>
            </div>
        </div>

        <!-- Knop -->
        <button
            type="submit"
            class="w-full mt-2 py-3 rounded-xl bg-[#123668] text-white text-xl font-semibold hover:bg-[#0d2a52] transition"
        >
            Wachtwoord instellen
        </button>
        <p>{{ status }}</p>
        </form>
    </div>
    </div>
</template>