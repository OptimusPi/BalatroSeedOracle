// Test script for Balatro Genie
import { BalatroGenie } from './src/genie.js';

async function runTests() {
    console.log('🧞 Testing Balatro Genie...\n');
    
    const genie = new BalatroGenie({
        aiProvider: 'local' // Use fallback generator for testing
    });
    
    const testPrompts = [
        "I want a Perkeo seed with Observatory",
        "Blueprint and negative jokers",
        "Lots of money early",
        "Turtle Bean with Blueprint and Burglar"
    ];
    
    for (const prompt of testPrompts) {
        console.log(`📝 Prompt: "${prompt}"`);
        const result = await genie.generateConfig(prompt);
        
        if (result.success) {
            console.log('✅ Success!');
            console.log(`   Name: ${result.config.name}`);
            console.log(`   Deck: ${result.config.deck}`);
            console.log(`   Must items: ${result.config.must.length}`);
            console.log(`   Should items: ${result.config.should.length}`);
        } else {
            console.log(`❌ Error: ${result.error}`);
        }
        console.log('');
    }
    
    console.log('🎉 Tests complete!');
}

runTests().catch(console.error);