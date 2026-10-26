# AI-Powered Search Recommendation

## Recommendation

For the four-week minimum viable product (MVP), Brownfield Consulting should implement hybrid search in Azure AI Search [1]. This combines ordinary keyword retrieval with vector retrieval using Azure OpenAI embeddings [2], [3], alongside structured filters for categories and tags. Optional semantic ranking [4] should be enabled only after relevance and cost evaluation.

Keyword search is effective for exact terms, names, acronyms, and specialist wording. Vector search converts text into numerical vectors (embeddings) that capture statistical patterns across related terms, helping find conceptually related content when wording differs [2], [3]. Embeddings do not understand meaning like a person; combining both approaches reduces the weaknesses of either method alone [1]. A consultant searching for guidance on difficult stakeholders should still find an article about client relationship management, even when the wording is different.

Users continue to receive normal search results, not AI-generated answers. The MVP should not introduce a chatbot or retrieval-augmented generation experience [5].

## Why This Fits Brownfield Consulting

Brownfield Consulting has approximately 200 consultants and 2,000 existing Word and PDF documents on a shared drive where current knowledge is difficult to search and reuse. Because authors and readers use different terminology, keyword search alone misses useful guidance, while vector search alone can miss exact acronyms or identifiers.

Hybrid search addresses this gap and supports a straightforward publishing workflow for subject-matter experts. Expected benefits include making useful content easier to find across differing terminology, which may reduce duplicated work and repeated requests to experienced consultants.

## What the User Experience Would Look Like

1. A consultant searches using normal language or specific keywords.
2. The system considers exact words and conceptual similarity across articles and extracted attachments [1], [6].
3. Category and tag filters narrow the results.
4. Upvotes and views provide small quality signals, but relevance remains primary.
5. Results show article summaries and useful matching excerpts.
6. The consultant opens the original article and attachments.

Azure SQL Database remains the authoritative article store, while Azure AI Search holds a rebuildable search index. Publishing an article does not wait for AI indexing; recently updated content may take a short time to appear in semantic search. If query vectorisation is unavailable, the system can fall back to keyword-only Azure AI Search. If Azure AI Search is unavailable, the existing SQL-backed search can provide basic title, content, tag, and category matching.

During authoring, AI can suggest one of the existing categories from the article title and content [7]. This is author assistance, not automated governance: the author must confirm or change the suggestion, the category remains optional, article publication does not depend on AI availability, and the model cannot create new categories automatically.

## Limitations and Controls

This approach does not guarantee perfect retrieval, so each limitation is paired with a proportionate mitigation [5]:

- **Plausible but irrelevant results**: Semantic similarity can return loosely related articles. *Mitigation*: Combine keyword and vector search [1], show source links and excerpts in every result, and gather consultant feedback on relevance.
- **Content and extraction quality**: Quality depends on document content, metadata, and extraction [6]; scanned or poorly formatted PDFs may require additional extraction or review. *Mitigation*: Run a pilot on a representative subset of documents and use human review for uncertain migration and category mappings.
- **Popularity bias**: Popularity signals can reinforce already popular content if weighted too strongly. *Mitigation*: Use small quality boosts rather than popularity-driven ranking, tested against representative search queries and expected results supplied by consultants.
- **Eventual indexing**: Newly published content is eventually indexed rather than instantly searchable. *Mitigation*: Keep Azure SQL authoritative for direct reads and monitor indexing failures and latency.
- **Dependencies, latency, and wrong suggestions**: AI services introduce latency, cost, and service dependencies, and category suggestions can be wrong [5]. *Mitigation*: Require author confirmation for categories, monitor zero-result searches and latency, enforce least-privilege access with managed identities, validate data residency and service regions before deployment, and maintain clear fallback behaviour when AI or search dependencies are unavailable.
- **No fact-checking or generated answers**: The system does not judge whether an article is factually correct or current, nor does the MVP produce generated answers or replace the source article. *Mitigation*: Link every result to its source article with author and date metadata.
- **Restricted content**: Restricted-document search would require security trimming before such content is indexed.

## Cost Considerations

Cost mainly depends on:

- Azure AI Search service tier and capacity
- number and size of documents and attachments
- attachment extraction requirements
- embedding generation during migration and subsequent updates [3]
- category suggestion usage [7] and semantic ranking usage [4]
- query volume and telemetry retention
- non-production environments and private networking requirements

The initial migration creates a one-off concentration of extraction and embedding work. Practical controls keep ongoing costs predictable:

- create embeddings only when content changes
- generate stored-content embeddings asynchronously
- request category suggestions only during authoring
- avoid a chat model call for every search
- begin with proportionate service capacity and monitor usage before scaling
- enable semantic ranking only if relevance testing justifies the extra cost [4]

## Proposed MVP Approach

Recommended MVP rollout sequence:

1. Validate service region, privacy, and data-residency constraints.
2. Select a representative pilot document set.
3. Agree a small benchmark set of real consultant queries and expected results.
4. Implement hybrid search and filters [1].
5. Compare keyword-only and hybrid results.
6. Tune quality boosts conservatively.
7. Migrate the remaining content in resumable batches after the pilot.
8. Monitor relevance, failures, latency, and usage during the demo and early adoption.

Practical MVP success indicators:

- consultants can find known relevant articles using different wording
- keyword-specific searches still behave accurately
- category and tag filters work predictably
- search results link back to source articles
- indexing failures are visible and recoverable
- users can report poor results
- search latency is acceptable based on an agreed measured target
- the approach remains understandable and supportable by Brownfield Consulting's team

## Decision

Proceed with hybrid Azure AI Search for the MVP, subject to an early validation spike for representative search relevance, document extraction quality, Azure region and model availability, data-residency requirements, and expected operating cost.

## Assumption

The exercise uses both Brownfield Consulting and Greenfield Consulting. Brownfield Consulting is treated as the intended customer name because that is the name used in the main scenario.

## References

1. Microsoft Learn: [Hybrid search in Azure AI Search](https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview)
2. Microsoft Learn: [Vector search overview in Azure AI Search](https://learn.microsoft.com/en-us/azure/search/vector-search-overview)
3. Microsoft Learn: [Generate embeddings with Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/embeddings)
4. Microsoft Learn: [Semantic ranking in Azure AI Search](https://learn.microsoft.com/en-us/azure/search/semantic-search-overview)
5. Microsoft Learn: [Transparency Note for Azure OpenAI Service](https://learn.microsoft.com/en-us/legal/cognitive-services/openai/transparency-note)
6. Microsoft Learn: [Index data from Azure Blob Storage in Azure AI Search](https://learn.microsoft.com/en-us/azure/search/search-howto-indexing-azure-blob-storage)
7. Microsoft Learn: [Structured Outputs with Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/structured-outputs)
