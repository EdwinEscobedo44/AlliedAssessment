using DedupTool;
using Xunit;

namespace DedupTool.Tests;

public class DeduperTests
{
    private static Document Loose(string id, string custodian, string hash) => new()
    {
        DocId = id,
        Custodian = custodian,
        DocType = "loose",
        FileName = id + ".txt",
        Sha256 = hash,
        SizeBytes = 10,
    };

    [Fact]
    public void IdenticalLooseDocuments_SecondIsSuppressed()
    {
        var batch = new Batch
        {
            Documents = new()
            {
                Loose("A", "c1", "h1"),
                Loose("B", "c2", "h1")
            },
        };

        var result = Deduper.Run(batch);

        Assert.Single(result.Rows);
        Assert.Equal("A", result.Rows[0].DocId);
        Assert.Equal(1, result.Suppressed);
    }

    [Fact]
    public void DistinctDocuments_AllKept()
    {
        var batch = new Batch
        {
            Documents = new()
            {
                Loose("A", "c1", "h1"),
                Loose("B", "c1", "h2")
            },
        };

        var result = Deduper.Run(batch);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(0, result.Suppressed);
    }

    [Fact]
    public void LoadFile_StartsWithFixedHeader()
    {
        var csv = LoadFileWriter.ToCsv(Array.Empty<LoadFileRow>());

        Assert.StartsWith(
            "DocID,FamilyID,ParentID,Custodian,DocType,FileName,SentDate,Hash",
            csv);
    }

    [Fact]
    public void EmailDates_AreConverted_ToUtc()
    {
        var batch = new Batch
        {
            Documents = new()
            {
                new Document
                {
                    DocId = "E1",
                    Custodian = "jsmith",
                    DocType = "email",
                    FileName = "mail.eml",
                    Sha256 = "hash1",
                    SentDate = "2024-03-05T14:22:00-05:00"
                }
            }
        };

        var result = Deduper.Run(batch);

        Assert.Equal(
            "2024-03-05T19:22:00Z",
            result.Rows[0].SentDate);
    }

    [Fact]
    public void AttachmentsInDifferentFamilies_ArePreserved()
    {
        var email1 = new Document
        {
            DocId = "E1",
            Custodian = "c1",
            DocType = "email",
            FileName = "email1.eml",
            Sha256 = "email1"
        };

        var attachment1 = new Document
        {
            DocId = "A1",
            Custodian = "c1",
            DocType = "attachment",
            ParentId = "E1",
            FileName = "report.pdf",
            Sha256 = "samehash"
        };

        var email2 = new Document
        {
            DocId = "E2",
            Custodian = "c2",
            DocType = "email",
            FileName = "email2.eml",
            Sha256 = "email2"
        };

        var attachment2 = new Document
        {
            DocId = "A2",
            Custodian = "c2",
            DocType = "attachment",
            ParentId = "E2",
            FileName = "report.pdf",
            Sha256 = "samehash"
        };

        var result = Deduper.Run(new Batch
        {
            Documents = new()
        {
            email1,
            attachment1,
            email2,
            attachment2
        }
        });

        Assert.Contains(result.Rows, r => r.DocId == "A1");
        Assert.Contains(result.Rows, r => r.DocId == "A2");

        Assert.Equal(4, result.Rows.Count);
        Assert.Equal(0, result.Suppressed);
    }
}