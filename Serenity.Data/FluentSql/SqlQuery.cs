﻿namespace Serenity.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    [DebuggerDisplay("{DebugText}")]
    public partial class SqlQuery : QueryWithParams, ISqlQuery, IFilterableQuery, IGetExpressionByName, ISqlQueryExtensible
    {
        private Dictionary<string, string> aliases;
        private List<Column> columns;
        private bool countRecords;
        private bool distinct;
        private StringBuilder from;
        private StringBuilder having;
        private StringBuilder groupBy;
        private List<string> orderBy;
        private string forXml;
        private int skip;
        private int take;
        private StringBuilder where;

        private Action<SqlQuery, string> ensureJoinsInExpression;
        private int intoIndex = -1;
        private List<object> into = new List<object>();

        /// <summary>
        /// Creates a new SqlQuery instance.
        /// </summary>
        public SqlQuery()
        {
            columns = new List<Column>();
            from = new StringBuilder();
        }

        /// <summary>
        /// Sets DISTINCT flag.
        /// </summary>
        /// <param name="distinct">Distinct flag.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Distinct(bool distinct)
        {
            this.distinct = distinct;

            return this;
        }

        public SqlQuery ForXml(string forXml)
        {
            this.forXml = forXml;
            return this;
        }

        /// <summary>
        /// Adds a table to the FROM statement. When it is called more than once, puts a comma
        /// between table names (cross join)
        /// </summary>
        /// <param name="table">Table name</param>
        /// <returns>The query itself.</returns>
        public SqlQuery From(string table)
        {
            if (string.IsNullOrEmpty(table))
                throw new ArgumentNullException("table");

            if (from.Length > 0)
                from.Append(", ");

            from.Append(table);

            return this;
        }

        /// <summary>
        /// Adds a table to the FROM statement with an alias. 
        /// When it is called more than once, puts a comma between table names (cross join)
        /// </summary>
        /// <param name="table">Table</param>
        /// <param name="alias">Alias for the table</param>
        /// <returns>The query itself.</returns>
        public SqlQuery From(string table, Alias alias)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (aliases != null &&
                aliases.ContainsKey(alias.Name))
                throw new ArgumentOutOfRangeException("{0} alias is used more than once in the query!");

            From(table);

            from.Append(' ');
            from.Append(alias.Name);

            ((ISqlQueryExtensible)this).Aliases.Add(alias.Name, table + " " + alias.Name);

            return this;
        }

        /// <summary>
        /// Adds a table to the FROM statement, with given short name.
        /// </summary>
        /// <param name="alias">Alias that contains table name and short name.</param>
        /// <returns>The query itself.</returns>
        /// <remarks>This overload requires that alias has a table name.</remarks>
        public SqlQuery From(Alias alias)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (string.IsNullOrEmpty(alias.Table))
                throw new ArgumentOutOfRangeException("alias.table");

            return From(alias.Table, alias);
        }

        /// <summary>
        /// Adds a subquery to the FROM statement, with given short name.
        /// </summary>
        /// <param name="subQuery">A subquery</param>
        /// <param name="alias">Alias that contains the short name.</param>
        /// <returns>The query itself.</returns>
        /// <remarks>This overload requires that alias has a table name.</remarks>
        public SqlQuery From(ISqlQuery subQuery, Alias alias)
        {
            if (subQuery == null)
                throw new ArgumentNullException("subQuery");

            if (alias == null)
                throw new ArgumentNullException("alias");

            return From(subQuery.ToString(), alias);
        }

        /// <summary>
        /// Gets the source expression for a column name in the query.
        /// </summary>
        /// <param name="columnName">Column name.</param>
        /// <returns>Expression or null if not found.</returns>
        /// <remarks>This function uses a linear search in column list, so use with caution.</remarks>
        string IGetExpressionByName.GetExpression(string columnName)
        {
            if (columnName == null)
                throw new ArgumentNullException("columnName");

            Column fieldInfo = columns.Find(
                column => (column.ColumnName != null && column.ColumnName == columnName) ||
                     (string.IsNullOrEmpty(column.ColumnName) && column.Expression == columnName));

            if (fieldInfo == null)
                return null;
            return fieldInfo.Expression;
        }

        /// <summary>
        /// Adds a field name or an SQL expression to the GROUP BY clause.
        /// </summary>
        /// <param name="expression">Array of fields or expressions.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery GroupBy(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException("expression");

            if (groupBy == null || groupBy.Length == 0)
                groupBy = new StringBuilder(expression);
            else
                groupBy.Append(", ").Append(expression);

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds a field of a given table alias to the GROUP BY clause.
        /// </summary>
        /// <param name="alias">A table alias that will be prepended to the field name with "." between</param>
        /// <param name="fieldName">A field name of the aliased table.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery GroupBy(Alias alias, string fieldName)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException("field");

            return GroupBy(alias + fieldName);
        }

        /// <summary>
        /// Adds an SQL expression to the GROUP BY clause.
        /// </summary>
        /// <param name="expression">Array of fields or expressions.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Having(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException("expression");

            if (having == null)
                having = new StringBuilder(expression);
            else
                having.Append(SqlKeywords.And).Append(expression);

            return this;
        }

        /// <summary>
        /// Adds a field name or an SQL expression to the ORDER BY clause.
        /// </summary>
        /// <param name="expression">A field or an SQL expression.</param>
        /// <param name="desc">True to add " DESC" keyword to the expression.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery OrderBy(string expression, bool desc = false)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException("field");

            if (desc)
                expression += SqlKeywords.Desc;

            if (orderBy == null)
                orderBy = new List<string>();

            orderBy.Add(expression);

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds a field of a given table alias to the ORDER BY clause.
        /// </summary>
        /// <param name="alias">A table alias that will be prepended to the field name with "." between</param>
        /// <param name="fieldName">A field name of the aliased table.</param>
        /// <param name="desc">True to add " DESC" keyword to the expression.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery OrderBy(Alias alias, string fieldName, bool desc = false)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException("field");

            return OrderBy(alias + fieldName, desc);
        }

        /// <summary>
        /// Inserts a field name or an SQL expression to the start of ORDER BY clause.
        /// </summary>
        /// <param name="expression">A field or an SQL expression.</param>
        /// <param name="desc">True to add a " DESC" suffix.</param>
        /// <returns>The query itself.</returns>
        /// <remarks>This method is designed to help apply user defined orders 
        /// (for example by clicking headers on a grid) to a query with
        /// existing order.
        /// SQL server throws an error if a field is used more than once in ORDER BY
        /// expression, so this function first removes normal and DESC versions of 
        /// the expression from the ORDER BY statement.
        /// </remarks>
        public SqlQuery OrderByFirst(string expression, bool desc = false)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException("field");

            if (desc)
                expression += SqlKeywords.Desc;

            if (orderBy == null)
                orderBy = new List<string>();

            string search = (expression ?? "").Trim();
            orderBy.RemoveAll(x => String.Compare((x ?? "").Trim(), search, StringComparison.OrdinalIgnoreCase) == 0);

            if (search.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase))
                search = search.Substring(0, search.Length - 5).Trim();
            else
                search += " DESC";

            orderBy.RemoveAll(x => String.Compare((x ?? "").Trim(), search, StringComparison.OrdinalIgnoreCase) == 0);

            if (orderBy.Count > 0)
                orderBy.Insert(0, expression);
            else
                orderBy.Add(expression);

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds a field name or an SQL expression to the SELECT statement.
        /// </summary>
        /// <param name="expression">A field or an SQL expression.</param>
        /// <returns>The query itself.</returns>
        /// <remarks>No column name is used for the field or expression.</remarks>
        public SqlQuery Select(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException("expression");

            columns.Add(new Column(expression, null, intoIndex, null));

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds a field of a given table alias to the SELECT statement.
        /// </summary>
        /// <param name="alias">A table alias that will be prepended to the field name with "." between</param>
        /// <param name="fieldName">A field name of the aliased table.</param>
        /// <returns>The query itself.</returns>
        /// <remarks>No column name is used for the field or expression.</remarks>
        public SqlQuery Select(Alias alias, string fieldName)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException("fieldName");

            string expression = alias + fieldName;

            columns.Add(new Column(expression, null, intoIndex, null));

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);
            return this;
        }

        /// <summary>
        /// Adds a field name or expression to the SELECT statement with a column name
        /// </summary>
        /// <param name="expression">A field name or SQL expression.</param>
        /// <param name="columnName">A column name.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Select(string expression, string columnName)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException("expression");

            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException("columnName");

            columns.Add(new Column(expression, columnName, intoIndex, null));

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds a field of a given table alias to the SELECT statement with a column name.
        /// </summary>
        /// <param name="alias">A table alias that will be prepended to the field name with "." between</param>
        /// <param name="fieldName">A field name of the aliased table.</param>
        /// <param name="columnName">A column name</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Select(Alias alias, string fieldName, string columnName)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException("fieldName");

            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException("columnName");

            var expression = alias + fieldName;

            columns.Add(new Column(expression, columnName, intoIndex, null));

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds a subquery to the SELECT statement.
        /// </summary>
        /// <param name="expression">A subquery.</param>
        /// <param name="columnName">A column name</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Select(ISqlQuery expression, string columnName)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException("columnName");

            this.Select(expression.ToString(), columnName);

            return this;
        }

        /// <summary>
        /// Adds a subquery to the SELECT statement.
        /// </summary>
        /// <param name="expression">A subquery.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Select(ISqlQuery expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            this.Select(expression.ToString());

            return this;
        }

        /// <summary>
        /// Adds field names or SQL expressions to the SELECT statement.
        /// </summary>
        /// <param name="expressions">Fields or SQL expressions.</param>
        /// <returns>The query itself.</returns>
        /// <remarks>No aliases are used for the fields or expressions.</remarks>
        public SqlQuery SelectMany(params string[] expressions)
        {
            foreach (var s in expressions)
                Select(s);

            return this;
        }

        /// <summary>
        /// Gets current SKIP value.
        /// </summary>
        /// <returns>SKIP value.</returns>
        public int Skip()
        {
            return skip;
        }

        /// <summary>
        /// Sets SKIP value. Used for paging.
        /// </summary>
        /// <param name="skipRows">Number of rows to skip (server dependant implementation)</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Skip(int skipRows)
        {
            skip = skipRows;
            return this;
        }


        /// <summary>
        /// Creates a new query that shares parameter dictionary with this query.
        /// </summary>
        /// <returns>
        /// A new query that shares parameters.</returns>
        public SqlQuery SubQuery()
        {
            var subQuery = new SqlQuery
            {
                parent = this
            };
            return subQuery;
        }

        /// <summary>
        /// Gets TAKE/TOP value.
        /// </summary>
        /// <returns>TAKE/TOP value.</returns>
        public int Take()
        {
            return take;
        }

        /// <summary>
        /// Sets TAKE/TOP value. Used for paging.
        /// </summary>
        /// <param name="rowCount">Number of rows to take.</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Take(int rowCount)
        {
            take = rowCount;
            return this;
        }

        /// <summary>
        /// Gets current query text.
        /// </summary>
        public string Text
        {
            get { return ToString(); }
        }

        /// <summary>
        /// Adds an expression to WHERE clause. If query already has a WHERE
        /// clause, inserts AND between existing one and new one.
        /// </summary>
        /// <param name="expression">An expression</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Where(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException(expression);

            if (where == null)
                where = new StringBuilder(expression);
            else
                where.Append(SqlKeywords.And).Append(expression);

            if (ensureJoinsInExpression != null)
                ensureJoinsInExpression(this, expression);

            return this;
        }

        /// <summary>
        /// Adds expressions to WHERE clause, inserting AND between them.
        /// </summary>
        /// <param name="expressions">An array of expressions</param>
        /// <returns>The query itself.</returns>
        public SqlQuery Where(params string[] expressions)
        {
            if (expressions == null || expressions.Length == 0)
                throw new ArgumentNullException("expressions");

            foreach (var expression in expressions)
                Where(expression);

            return this;
        }

        /// <summary>
        /// Implements IDBFilterable.Where, by calling original Where method.
        /// </summary>
        /// <param name="expression">An expression</param>
        void IFilterableQuery.Where(string expression)
        {
            this.Where(expression);
        }

        /// <summary>
        /// Gets the dialect (SQL server type / version) for query.
        /// </summary>
        public SqlDialect Dialect()
        {
            return this.dialect;
        }

        /// <summary>
        /// Sets the dialect (SQL server type / version) for query.
        /// </summary>
        /// <remarks>TODO: SqlDialect system should be improved.</remarks>
        public SqlQuery Dialect(SqlDialect dialect)
        {
            this.dialect = dialect;

            return this;
        }


        /// <summary>
        /// Gets/sets the flag to get the total record count when paging is used by SKIP/TAKE. 
        /// A secondary query without SKIP/TAKE is generated to get total record count, 
        /// when this property is true.
        /// </summary>
        public bool CountRecords
        {
            get { return countRecords; }
            set { countRecords = value; }
        }

        object ISqlQueryExtensible.FirstIntoRow
        {
            get { return into.Count > 0 ? into[0] : null; }
        }

        IList<Column> ISqlQueryExtensible.Columns
        {
            get { return columns; }
        }

        IList<object> ISqlQueryExtensible.IntoRows
        {
            get { return into; }
        }

        IDictionary<string, string> ISqlQueryExtensible.Aliases 
        {
            get
            {
                if (aliases == null)
                    aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                return aliases;
            }
        }

        int ISqlQueryExtensible.GetSelectIntoIndex(IField field)
        {
            return this.columns.FindIndex(
                delegate(Column s) { return s.IntoField == field; });
        }

        Action<SqlQuery, string> ISqlQueryExtensible.EnsureJoinsInExpression
        {
            get { return ensureJoinsInExpression; }
            set { ensureJoinsInExpression = value; }
        }

        void ISqlQueryExtensible.IntoRowSelection(object row)
        {
            if (row == null)
                intoIndex = -1;
            else
            {
                intoIndex = into.IndexOf(row);
                if (intoIndex == -1)
                {
                    into.Add(row);
                    intoIndex = into.Count - 1;
                }
            }
        }

        /// <summary>
        /// Holds information about a column in SELECT clause.
        /// </summary>
        public class Column
        {
            /// <summary>Field or expression</summary>
            public readonly string Expression;
            /// <summary>Column name</summary>
            public readonly string ColumnName;
            /// <summary>Used by entity system when more than one entity is used as a target</summary>
            public readonly int IntoRowIndex;
            /// <summary>Used by entity system, to determine which field this column value will be read into</summary>
            public readonly object IntoField;

            public Column(string expression, string columnName, int intoRow, object intoField)
            {
                this.Expression = expression;
                this.ColumnName = columnName;
                this.IntoRowIndex = intoRow;
                this.IntoField = intoField;
            }

            public Column(SqlQuery query, string expression, string columnName, object intoField)
                : this(expression, columnName, query.intoIndex, intoField)
            {
                query.columns.Add(this);
            }
        }
    }
}